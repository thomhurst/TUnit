namespace TUnit.Core.Exceptions;

/// <summary>
/// Exception thrown when a test dependency has failed and the test cannot proceed
/// </summary>
public class TestDependencyException : TUnitException
{
    /// <summary>
    /// Name of the dependency that failed
    /// </summary>
    public string DependencyName { get; }
    
    /// <summary>
    /// Whether the test should proceed despite the dependency failure
    /// </summary>
    public bool ProceedOnFailure { get; }
    
    public TestDependencyException(string dependencyName, bool proceedOnFailure) 
        : base($"Dependency failed: {dependencyName}")
    {
        DependencyName = dependencyName;
        ProceedOnFailure = proceedOnFailure;
    }
}