using PepperX.QueryForge.Dapper.Compiler;

namespace PepperX.QueryForge.Dapper.Dialects;

/// <summary>PostgreSQL.</summary>
public sealed class PostgreSqlDialect : ISqlDialect
{
    /// <inheritdoc />
    public DapperDatabaseProvider ProviderType => DapperDatabaseProvider.PostgreSQL;

    /// <inheritdoc />
    public string DefaultSchema => "public";

    /// <inheritdoc />
    public bool RequiresOrderByForPaging => false;

    /// <inheritdoc />
    public string OrderByFallback => "1";

    /// <inheritdoc />
    public string LikeEscapeClause => @" ESCAPE '\'";

    /// <inheritdoc />
    public bool SupportsTableValuedFunctions => true;

    /// <inheritdoc />
    public bool SupportsStoredProcedures => true;

    /// <summary>
    /// Stated explicitly, because this engine defaults to nulls last ascending while most others
    /// default to nulls first.
    /// </summary>
    public string NullOrdering(SortOrder order)
        => order == SortOrder.Descending ? " NULLS LAST" : " NULLS FIRST";

    /// <inheritdoc />
    public string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }

    /// <inheritdoc />
    public string ParameterReference(string name) => "@" + name;

    /// <inheritdoc />
    public string PagingClause(int offset, int size) => $"LIMIT {size} OFFSET {offset}";

    /// <inheritdoc />
    public string EscapeLikeValue(string value)
        => value
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_");

    /// <inheritdoc />
    public string BuildSource(
        string schema,
        string name,
        DapperObjectType type,
        IReadOnlyList<string> argumentReferences)
    {
        var qualified = QuoteIdentifier(schema) + "." + QuoteIdentifier(name);

        // A set-returning function is selected from exactly like a table.
        return type == DapperObjectType.TVF
            ? $"{qualified}({string.Join(", ", argumentReferences)})"
            : qualified;
    }

    /// <inheritdoc />
    public string BuildStoredProcedureCall(
        string schema,
        string name,
        IReadOnlyList<KeyValuePair<string, string>> argumentNames)
    {
        var qualified = QuoteIdentifier(schema) + "." + QuoteIdentifier(name);

        // Named notation, so a caller's parameter order does not have to match the declaration.
        var arguments = argumentNames.Select(a => $"{a.Key} => {a.Value}");

        return $"SELECT * FROM {qualified}({string.Join(", ", arguments)})";
    }
}
