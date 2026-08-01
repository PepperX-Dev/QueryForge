using System.Collections.Concurrent;
using System.Reflection;

namespace PepperX.QueryForge.Querying;

/// <summary>
/// Reads a named column value off a materialized model by property name.
/// </summary>
/// <remarks>
/// QueryForge addresses data by column name, but providers hand back POCOs. This bridges the two
/// for the common case where the model's property names match the column names, which is what both
/// Dapper's and EF Core's default materialization produce. Callers with a different mapping can
/// supply their own accessor instead.
/// <para>Lookups are case-insensitive and the reflection metadata is cached per type.</para>
/// </remarks>
public static class PropertyAccessor
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> Cache = new();

    /// <summary>
    /// Returns an accessor for <typeparamref name="TModel"/>. Unknown column names read as
    /// <see langword="null"/> rather than throwing, so a stray column cannot break a whole query.
    /// </summary>
    public static Func<TModel, string, object?> For<TModel>() => Read;

    /// <summary>
    /// Whether <typeparamref name="TModel"/> actually has a readable property with this name.
    /// </summary>
    /// <remarks>
    /// Callers need this to tell "the column holds null" apart from "there is no such column". The
    /// two must behave differently: a condition on a column that does not exist is dropped, in the
    /// same way the SQL providers drop names missing from their whitelist, rather than quietly
    /// matching nothing.
    /// </remarks>
    public static bool Exists<TModel>(string? columnName)
        => !string.IsNullOrWhiteSpace(columnName) && MapFor(typeof(TModel)).ContainsKey(columnName);

    private static object? Read<TModel>(TModel row, string columnName)
    {
        if (row is null)
            return null;

        // The runtime type is used rather than TModel so that proxies and derived types resolve.
        if (!MapFor(row.GetType()).TryGetValue(columnName, out var match))
            return null;

        var value = match.GetValue(row);

        return value is DBNull ? null : value;
    }

    private static Dictionary<string, PropertyInfo> MapFor(Type type)
        => Cache.GetOrAdd(type, static t =>
        {
            var properties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanRead && property.GetIndexParameters().Length == 0)
                    properties[property.Name] = property;
            }

            return properties;
        });
}
