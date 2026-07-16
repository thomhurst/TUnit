namespace TUnit.Assertions.Core;

/// <summary>
/// Specifies how assertions should be combined in a chain.
/// </summary>
internal enum CombinerType
{
    /// <summary>
    /// All assertions in the chain must pass (AND logic).
    /// </summary>
    And,

    /// <summary>
    /// At least one assertion in the chain must pass (OR logic).
    /// </summary>
    Or
}
