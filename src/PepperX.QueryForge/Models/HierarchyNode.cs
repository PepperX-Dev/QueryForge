namespace PepperX.QueryForge;

/// <summary>
/// Represents a single node in a grouped, hierarchical query result.
/// </summary>
/// <typeparam name="TModel">The domain model type for the leaf items.</typeparam>
public record HierarchyNode<TModel>(
    object? Key,
    int Count,
    IReadOnlyList<HierarchyNode<TModel>>? SubGroups,
    IReadOnlyList<TModel>? Items
);