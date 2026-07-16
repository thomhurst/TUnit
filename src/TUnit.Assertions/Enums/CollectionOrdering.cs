namespace TUnit.Assertions.Enums;

/// <summary>
/// Specifies how collections should be compared for equivalency.
/// </summary>
public enum CollectionOrdering
{
    /// <summary>
    /// Collections are equivalent if they contain the same elements in any order.
    /// </summary>
    Any,

    /// <summary>
    /// Collections are equivalent only if they contain the same elements in the exact same order.
    /// </summary>
    Matching
}
