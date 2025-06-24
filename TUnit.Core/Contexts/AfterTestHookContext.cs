using System;
using TUnit.Core.Enums;

namespace TUnit.Core.Contexts;

/// <summary>
/// Context used for after test hooks
/// </summary>
public sealed class AfterTestHookContext : ContextBase
{
    /// <summary>
    /// Details about the test that just ran
    /// </summary>
    public TestDetails TestDetails { get; }
    
    /// <summary>
    /// The test instance (if instance method)
    /// </summary>
    public object? TestInstance { get; }
    
    /// <summary>
    /// The result of the test execution
    /// </summary>
    public TestResult TestResult { get; }
    
    /// <summary>
    /// The status of the test
    /// </summary>
    public Status TestStatus => TestResult.Status;
    
    /// <summary>
    /// Exception thrown by the test (if failed)
    /// </summary>
    public Exception? Exception => TestResult.Exception;
    
    /// <summary>
    /// Duration of the test execution
    /// </summary>
    public TimeSpan? Duration => TestResult.Duration;
    
    public AfterTestHookContext(TestDetails testDetails, object? testInstance, TestResult testResult)
    {
        TestDetails = testDetails ?? throw new ArgumentNullException(nameof(testDetails));
        TestInstance = testInstance;
        TestResult = testResult ?? throw new ArgumentNullException(nameof(testResult));
    }
}