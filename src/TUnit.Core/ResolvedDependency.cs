namespace TUnit.Core;

/// <summary>
/// Represents a resolved test dependency with its associated metadata.
/// This maintains the relationship between a dependency test and its configuration (e.g., ProceedOnFailure).
/// </summary>
public sealed class ResolvedDependency
{
    /// <summary>
    /// The resolved test that this test depends on.
    /// </summary>
    public required AbstractExecutableTest Test { get; init; }
    
    /// <summary>
    /// The original dependency metadata, including configuration like ProceedOnFailure.
    /// </summary>
    public required TestDependency Metadata { get; init; }
    
    /// <summary>
    /// Quick access to the ProceedOnFailure flag from the metadata.
    /// </summary>
    public bool ProceedOnFailure => Metadata.ProceedOnFailure;
}