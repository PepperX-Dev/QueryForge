namespace PepperX.QueryForge;

/// <summary>
/// Represents the total count metrics returned by the execution provider.
/// </summary>
public record QueryResultMetaTotal(int Rows, int Pages);

/// <summary>
/// Represents the metadata envelope for a query result.
/// </summary>
public record QueryResultMeta(QueryResultMetaTotal Total, QueryResultType Type);