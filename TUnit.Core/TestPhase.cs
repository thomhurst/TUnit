namespace TUnit.Core;

/// <summary>
/// Represents the phase of test execution
/// </summary>
public enum TestPhase
{
    /// <summary>
    /// Test discovery phase
    /// </summary>
    Discovery,
    
    /// <summary>
    /// Test registration phase
    /// </summary>
    Registration,
    
    /// <summary>
    /// Before test execution
    /// </summary>
    BeforeTest,
    
    /// <summary>
    /// During test execution
    /// </summary>
    Execution,
    
    /// <summary>
    /// After test execution
    /// </summary>
    AfterTest
}