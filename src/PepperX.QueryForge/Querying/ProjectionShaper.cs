using System.Collections.Concurrent;
using System.Reflection;

namespace PepperX.QueryForge.Querying;

/// <summary>
/// Applies <see cref="Query.SelectColumns"/> to already-materialized models.
/// </summary>
/// <remarks>
/// The SQL providers narrow the SELECT list so unselected columns are never fetched. Providers that
/// materialize whole objects use this instead, so that the caller sees the same thing either way: a
/// model carrying only the requested columns, with everything else left at its default.
/// <para>
/// Shaping needs a parameterless constructor and settable properties. A model without them is
/// returned untouched rather than throwing — a projection is a bandwidth optimization, and failing a
/// whole query over one is the wrong trade.
/// </para>
/// </remarks>
public static class ProjectionShaper
{
    private static readonly ConcurrentDictionary<Type, ShapeInfo> Cache = new();

    /// <summary>
    /// Returns a copy of <paramref name="source"/> carrying only the selected columns.
    /// </summary>
    /// <param name="source">The fully-materialized model.</param>
    /// <param name="selectColumns">The requested columns. Empty means "everything", so nothing is done.</param>
    /// <param name="alsoKeep">
    /// Extra columns to preserve regardless of the selection — the grouping columns, which the
    /// hierarchy is rebuilt from and which the SQL providers also force into the SELECT list.
    /// </param>
    public static TModel Shape<TModel>(
        TModel source,
        IReadOnlyList<string> selectColumns,
        IReadOnlyList<string>? alsoKeep = null)
    {
        if (source is null || selectColumns.Count == 0)
            return source;

        var info = Cache.GetOrAdd(source.GetType(), static type => new ShapeInfo(type));

        if (!info.CanShape)
            return source;

        var wanted = new HashSet<string>(selectColumns, StringComparer.OrdinalIgnoreCase);

        if (alsoKeep is not null)
        {
            foreach (var column in alsoKeep)
                wanted.Add(column);
        }

        // A selection naming nothing real would blank the whole model, which is never what was meant.
        if (!info.Properties.Keys.Any(wanted.Contains))
            return source;

        var shaped = (TModel)info.Create();

        foreach (var (name, property) in info.Properties)
        {
            if (wanted.Contains(name))
                property.SetValue(shaped, property.GetValue(source));
        }

        return shaped;
    }

    /// <summary>Shapes a whole sequence. Returns the input untouched when nothing is selected.</summary>
    /// <remarks>
    /// Deliberately named differently from <see cref="Shape{TModel}(TModel, IReadOnlyList{string}, IReadOnlyList{string})"/>:
    /// as an overload, a call passing a list would bind to the single-item version with
    /// <c>TModel</c> inferred as the list type, and silently shape nothing.
    /// </remarks>
    public static IReadOnlyList<TModel> ShapeAll<TModel>(
        IReadOnlyList<TModel> rows,
        IReadOnlyList<string> selectColumns,
        IReadOnlyList<string>? alsoKeep = null)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (selectColumns.Count == 0 || rows.Count == 0)
            return rows;

        var shaped = new List<TModel>(rows.Count);

        foreach (var row in rows)
            shaped.Add(Shape(row, selectColumns, alsoKeep));

        return shaped;
    }

    /// <summary>
    /// Rebuilds a hierarchy with its leaf items shaped, leaving keys and counts untouched.
    /// </summary>
    public static IReadOnlyList<HierarchyNode<TModel>> ShapeHierarchy<TModel>(
        IReadOnlyList<HierarchyNode<TModel>> nodes,
        IReadOnlyList<string> selectColumns,
        IReadOnlyList<string>? alsoKeep = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        if (selectColumns.Count == 0)
            return nodes;

        var result = new List<HierarchyNode<TModel>>(nodes.Count);

        foreach (var node in nodes)
        {
            result.Add(new HierarchyNode<TModel>(
                node.Key,
                node.Count,
                node.SubGroups is null ? null : ShapeHierarchy(node.SubGroups, selectColumns, alsoKeep),
                node.Items is null ? null : ShapeAll(node.Items, selectColumns, alsoKeep)));
        }

        return result;
    }

    private sealed class ShapeInfo
    {
        public ShapeInfo(Type type)
        {
            Properties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
                    Properties[property.Name] = property;
            }

            Constructor = type.GetConstructor(Type.EmptyTypes);
            CanShape = Constructor is not null && Properties.Count > 0;
        }

        public Dictionary<string, PropertyInfo> Properties { get; }

        private ConstructorInfo? Constructor { get; }

        public bool CanShape { get; }

        public object Create() => Constructor!.Invoke(null);
    }
}
