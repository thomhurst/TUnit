using System;

namespace TUnit.Core.Contexts;

/// <summary>
/// Context used for before test hooks
/// </summary>
public sealed class BeforeTestHookContext : ContextBase
{
    /// <summary>
    /// Details about the test that is about to run
    /// </summary>
    public TestDetails TestDetails { get; }
    
    /// <summary>
    /// The test instance (if instance method)
    /// </summary>
    public object? TestInstance { get; }
    
    /// <summary>
    /// Whether the test should be skipped
    /// </summary>
    public bool ShouldSkip { get; private set; }
    
    /// <summary>
    /// Reason for skipping (if ShouldSkip is true)
    /// </summary>
    public string? SkipReason { get; private set; }
    
    public BeforeTestHookContext(TestDetails testDetails, object? testInstance)
    {
        TestDetails = testDetails ?? throw new ArgumentNullException(nameof(testDetails));
        TestInstance = testInstance;
    }
    
    /// <summary>
    /// Marks the test to be skipped with the given reason
    /// </summary>
    public void SkipTest(string reason)
    {
        ShouldSkip = true;
        SkipReason = reason ?? "Test skipped by before hook";
    }
}