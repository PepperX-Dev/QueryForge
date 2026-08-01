using PepperX.QueryForge.Dapper.Compiler;

namespace PepperX.QueryForge.Dapper.Dialects;

/// <summary>SQLite.</summary>
/// <remarks>
/// Useful in its own right for desktop and embedded applications, and useful in a test suite: it
/// exercises the whole compile-and-execute path in-process, without a database server.
/// </remarks>
public sealed class SqliteDialect : ISqlDialect
{
    /// <inheritdoc />
    public DapperDatabaseProvider ProviderType => DapperDatabaseProvider.SQLite;

    /// <summary>SQLite has no schemas beyond attached databases.</summary>
    public string DefaultSchema => string.Empty;

    /// <inheritdoc />
    public bool RequiresOrderByForPaging => false;

    /// <inheritdoc />
    public string OrderByFallback => "1";

    /// <inheritdoc />
    public string LikeEscapeClause => @" ESCAPE '\'";

    /// <summary>SQLite has no table-valued functions.</summary>
    public bool SupportsTableValuedFunctions => false;

    /// <summary>SQLite has no stored procedures.</summary>
    public bool SupportsStoredProcedures => false;

    /// <summary>SQLite already sorts nulls first ascending.</summary>
    public string NullOrdering(SortOrder order) => string.Empty;

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
        if (type == DapperObjectType.TVF)
            throw new NotSupportedException("SQLite has no table-valued functions.");

        return string.IsNullOrWhiteSpace(schema)
            ? QuoteIdentifier(name)
            : QuoteIdentifier(schema) + "." + QuoteIdentifier(name);
    }

    /// <inheritdoc />
    public string BuildStoredProcedureCall(
        string schema,
        string name,
        IReadOnlyList<KeyValuePair<string, string>> argumentNames)
        => throw new NotSupportedException("SQLite has no stored procedures.");
}
