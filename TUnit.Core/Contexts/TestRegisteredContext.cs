using System;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Contexts;

/// <summary>
/// Context used after test discovery but before test execution
/// Allows setting execution-related properties like custom executors and parallel limits
/// </summary>
public sealed class TestRegisteredContext : ContextBase
{
    /// <summary>
    /// The test that has been registered
    /// </summary>
    public TestDetails TestDetails { get; }
    
    /// <summary>
    /// The discovered test instance (if available)
    /// </summary>
    public DiscoveredTest? DiscoveredTest { get; internal set; }
    
    public TestRegisteredContext(TestDetails testDetails)
    {
        TestDetails = testDetails ?? throw new ArgumentNullException(nameof(testDetails));
    }
    
    /// <summary>
    /// Sets a custom test executor for this test
    /// </summary>
    public void SetTestExecutor(ITestExecutor executor)
    {
        if (executor == null)
            throw new ArgumentNullException(nameof(executor));
            
        Items["TestExecutor"] = executor;
        
        if (DiscoveredTest != null)
        {
            DiscoveredTest.TestExecutor = executor;
        }
    }
    
    /// <summary>
    /// Sets the parallel limiter for this test
    /// </summary>
    public void SetParallelLimiter(IParallelLimit parallelLimit)
    {
        if (parallelLimit == null)
            throw new ArgumentNullException(nameof(parallelLimit));
            
        Items["ParallelLimiter"] = parallelLimit;
    }
}