using PepperX.QueryForge.Dapper.Compiler;

namespace PepperX.QueryForge.Dapper.Dialects;

/// <summary>Microsoft SQL Server.</summary>
public sealed class SqlServerDialect : ISqlDialect
{
    /// <inheritdoc />
    public DapperDatabaseProvider ProviderType => DapperDatabaseProvider.MSSQL;

    /// <inheritdoc />
    public string DefaultSchema => "dbo";

    /// <inheritdoc />
    public bool RequiresOrderByForPaging => true;

    /// <inheritdoc />
    public string OrderByFallback => "(SELECT NULL)";

    /// <inheritdoc />
    public string LikeEscapeClause => @" ESCAPE '\'";

    /// <inheritdoc />
    public bool SupportsTableValuedFunctions => true;

    /// <inheritdoc />
    public bool SupportsStoredProcedures => true;

    /// <summary>SQL Server already sorts nulls first ascending.</summary>
    public string NullOrdering(SortOrder order) => string.Empty;

    /// <inheritdoc />
    public string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        return "[" + identifier.Replace("]", "]]") + "]";
    }

    /// <inheritdoc />
    public string ParameterReference(string name) => "@" + name;

    /// <inheritdoc />
    public string PagingClause(int offset, int size)
        => $"OFFSET {offset} ROWS FETCH NEXT {size} ROWS ONLY";

    /// <inheritdoc />
    public string EscapeLikeValue(string value)
        => value
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_")
            .Replace("[", @"\[");

    /// <inheritdoc />
    public string BuildSource(
        string schema,
        string name,
        DapperObjectType type,
        IReadOnlyList<string> argumentReferences)
    {
        var qualified = QuoteIdentifier(schema) + "." + QuoteIdentifier(name);

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

        if (argumentNames.Count == 0)
            return $"EXEC {qualified}";

        // Named arguments, so a caller's parameter order does not have to match the procedure's.
        var arguments = argumentNames.Select(a => $"@{a.Key} = {a.Value}");

        return $"EXEC {qualified} {string.Join(", ", arguments)}";
    }
}
