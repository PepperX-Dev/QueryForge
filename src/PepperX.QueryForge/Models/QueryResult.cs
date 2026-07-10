namespace PepperX.QueryForge;

/// <summary>Represents a single filtering condition applied to a specific column.</summary>
/// <summary>
/// The standardized, strongly-typed result returned by any QueryForge execution provider.
/// </summary>
/// <typeparam name="TModel">The domain model type representing a single data row.</typeparam>
public record QueryResult<TModel>
{
    /// <summary>The metadata containing total counts and the result shape type.</summary>
    public QueryResultMeta Meta { get; init; } = new(new(0, 0), QueryResultType.Flat);

    /// <summary>
    /// The flat list of models. Populated when <see cref="Meta.Type"/> is <see cref="QueryResultType.Flat"/>.
    /// Guaranteed to be non-null (defaults to empty array).
    /// </summary>
    public IReadOnlyList<TModel> Models { get; init; } = Array.Empty<TModel>();

    /// <summary>
    /// The hierarchical tree of models. Populated when <see cref="Meta.Type"/> is <see cref="QueryResultType.Grouped"/>.
    /// Guaranteed to be non-null (defaults to empty array).
    /// </summary>
    public IReadOnlyList<HierarchyNode<TModel>> Groups { get; init; } = Array.Empty<HierarchyNode<TModel>>();
}