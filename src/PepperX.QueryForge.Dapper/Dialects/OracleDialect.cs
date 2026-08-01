using PepperX.QueryForge.Dapper.Compiler;

namespace PepperX.QueryForge.Dapper.Dialects;

/// <summary>
/// Oracle Database 12c and later, which is where <c>OFFSET … FETCH NEXT</c> became available.
/// </summary>
/// <remarks>
/// Oracle folds unquoted identifiers to upper case, and QueryForge quotes what it is given. Column
/// names are safe because they are discovered from the live result set and therefore already carry
/// the stored casing; object names come from your code, so write them the way Oracle stored them —
/// usually upper case.
/// </remarks>
public sealed class OracleDialect : ISqlDialect
{
    /// <inheritdoc />
    public DapperDatabaseProvider ProviderType => DapperDatabaseProvider.Oracle;

    /// <summary>Oracle scopes objects by owner, which has no portable default.</summary>
    public string DefaultSchema => string.Empty;

    /// <inheritdoc />
    public bool RequiresOrderByForPaging => false;

    /// <inheritdoc />
    public string OrderByFallback => "1";

    /// <inheritdoc />
    public string LikeEscapeClause => @" ESCAPE '\'";

    /// <inheritdoc />
    public bool SupportsTableValuedFunctions => true;

    /// <summary>
    /// An Oracle procedure returns a result set through a REF CURSOR output parameter rather than as
    /// a statement result, which cannot be expressed as portable command text.
    /// </summary>
    public bool SupportsStoredProcedures => false;

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
    public string ParameterReference(string name) => ":" + name;

    /// <inheritdoc />
    public string PagingClause(int offset, int size)
        => $"OFFSET {offset} ROWS FETCH NEXT {size} ROWS ONLY";

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
        var qualified = Qualify(schema, name);

        // A pipelined or collection-returning function has to be unnested with TABLE().
        return type == DapperObjectType.TVF
            ? $"TABLE({qualified}({string.Join(", ", argumentReferences)}))"
            : qualified;
    }

    /// <inheritdoc />
    public string BuildStoredProcedureCall(
        string schema,
        string name,
        IReadOnlyList<KeyValuePair<string, string>> argumentNames)
        => throw new NotSupportedException(
            "Oracle procedures return result sets through REF CURSOR output parameters. " +
            "Wrap the logic in a pipelined function and query it with DapperObjectType.TVF instead.");

    private string Qualify(string schema, string name)
        => string.IsNullOrWhiteSpace(schema)
            ? QuoteIdentifier(name)
            : QuoteIdentifier(schema) + "." + QuoteIdentifier(name);
}
