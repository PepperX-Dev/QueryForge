using PepperX.QueryForge.Dapper.Compiler;

namespace PepperX.QueryForge.Dapper.Dialects;

/// <summary>MySQL and MariaDB.</summary>
public sealed class MySqlDialect : ISqlDialect
{
    /// <inheritdoc />
    public DapperDatabaseProvider ProviderType => DapperDatabaseProvider.MySQL;

    /// <summary>
    /// MySQL has no schema layer above the database, so the schema part of a query object is left
    /// off unless the caller sets one explicitly.
    /// </summary>
    public string DefaultSchema => string.Empty;

    /// <inheritdoc />
    public bool RequiresOrderByForPaging => false;

    /// <inheritdoc />
    public string OrderByFallback => "1";

    /// <inheritdoc />
    public string LikeEscapeClause => @" ESCAPE '\\'";

    /// <summary>MySQL has no table-valued functions.</summary>
    public bool SupportsTableValuedFunctions => false;

    /// <inheritdoc />
    public bool SupportsStoredProcedures => true;

    /// <summary>MySQL already sorts nulls first ascending.</summary>
    public string NullOrdering(SortOrder order) => string.Empty;

    /// <inheritdoc />
    public string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        return "`" + identifier.Replace("`", "``") + "`";
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
            throw new NotSupportedException("MySQL has no table-valued functions.");

        return Qualify(schema, name);
    }

    /// <inheritdoc />
    public string BuildStoredProcedureCall(
        string schema,
        string name,
        IReadOnlyList<KeyValuePair<string, string>> argumentNames)
    {
        // CALL takes positional arguments only, so they are passed in the order supplied.
        var arguments = argumentNames.Select(a => a.Value);

        return $"CALL {Qualify(schema, name)}({string.Join(", ", arguments)})";
    }

    private string Qualify(string schema, string name)
        => string.IsNullOrWhiteSpace(schema)
            ? QuoteIdentifier(name)
            : QuoteIdentifier(schema) + "." + QuoteIdentifier(name);
}
