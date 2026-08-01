namespace PepperX.QueryForge.InMemory;

/// <summary>
/// Ready-made value accessors for sources whose columns are not plain properties.
/// </summary>
/// <remarks>
/// The default accessor reads properties by name, which covers ordinary POCOs. These cover the other
/// common shapes without needing a wrapper type.
/// </remarks>
public static class InMemoryAccessors
{
    /// <summary>
    /// Reads columns out of a dictionary row, case-insensitively.
    /// </summary>
    /// <example>
    /// <code>
    /// var result = rows.ToQueryResult(query, InMemoryAccessors.ForDictionary());
    /// </code>
    /// </example>
    public static Func<TRow, string, object?> ForDictionary<TRow>()
        where TRow : IReadOnlyDictionary<string, object?>
        => static (row, column) =>
        {
            if (row is null)
                return null;

            foreach (var (key, value) in row)
            {
                if (string.Equals(key, column, StringComparison.OrdinalIgnoreCase))
                    return value is DBNull ? null : value;
            }

            return null;
        };

    /// <summary>
    /// Maps caller-facing column names onto different property names before reading.
    /// </summary>
    /// <param name="columnToProperty">
    /// Column name to property name. Names absent from the map are read as-is.
    /// </param>
    /// <remarks>
    /// Useful when the names a client sends are part of your API contract and should not be forced
    /// to track how the model happens to be written.
    /// </remarks>
    public static Func<TModel, string, object?> WithColumnMap<TModel>(
        IReadOnlyDictionary<string, string> columnToProperty)
    {
        ArgumentNullException.ThrowIfNull(columnToProperty);

        var map = new Dictionary<string, string>(columnToProperty, StringComparer.OrdinalIgnoreCase);
        var inner = Querying.PropertyAccessor.For<TModel>();

        return (row, column) => inner(row, map.GetValueOrDefault(column, column));
    }
}
