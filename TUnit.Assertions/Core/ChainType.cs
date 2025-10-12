namespace TUnit.Assertions.Core;

/// <summary>
/// Represents the type of logical connection between assertions.
/// </summary>
public enum ChainType
{
    /// <summary>
    /// No chaining - standalone assertion.
    /// </summary>
    None,

    /// <summary>
    /// And - both assertions must pass.
    /// </summary>
    And,

    /// <summary>
    /// Or - at least one assertion must pass.
    /// </summary>
    Or
}
