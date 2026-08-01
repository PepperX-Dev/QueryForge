namespace PepperX.QueryForge.Dapper.Compiler;

/// <summary>
/// The columns a query is allowed to reference on a given object, and the type of each.
/// </summary>
/// <remarks>
/// This is QueryForge's defence against a caller using filters, sorts, or projections to discover or
/// reach columns they were never offered. Names are discovered from the object's real result set, so
/// the list reflects what the database actually exposes rather than what a caller claims. Anything
/// not on the list is dropped by the compiler rather than escaped and emitted, which keeps an unknown
/// column from reaching the database at all.
/// <para>
/// The types matter just as much. A filter value arriving from JSON is often text even when the
/// column is numeric or a date, and a strict engine rejects that comparison outright rather than
/// guessing — so the compiler coerces each value to its column's type before binding it.
/// </para>
/// </remarks>
public sealed class ColumnWhitelist
{
    private readonly Dictionary<string, Type?> _columns;

    /// <summary>Creates a whitelist from column names alone, leaving types unknown.</summary>
    public ColumnWhitelist(IEnumerable<string> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);

        _columns = new Dictionary<string, Type?>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in columns)
            _columns[column] = null;
    }

    /// <summary>Creates a whitelist from the columns and types of a result set.</summary>
    public ColumnWhitelist(IEnumerable<KeyValuePair<string, Type?>> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);

        _columns = new Dictionary<string, Type?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, type) in columns)
            _columns[name] = type;
    }

    /// <summary>The allowed column names.</summary>
    public IReadOnlyCollection<string> Columns => _columns.Keys;

    /// <summary>Whether a caller-supplied name refers to a real column.</summary>
    public bool Contains(string? columnName)
        => !string.IsNullOrWhiteSpace(columnName) && _columns.ContainsKey(columnName);

    /// <summary>
    /// The CLR type the database reports for a column, or <see langword="null"/> when it is unknown.
    /// </summary>
    public Type? TypeOf(string? columnName)
        => !string.IsNullOrWhiteSpace(columnName) && _columns.TryGetValue(columnName, out var type)
            ? type
            : null;
}
