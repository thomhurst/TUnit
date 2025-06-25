using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Service for managing test registry and dynamic test registration
/// </summary>
public class TestRegistry
{
    private readonly ConcurrentDictionary<string, ExecutableTest> _allTests = new();
    private readonly ConcurrentDictionary<string, List<ExecutableTest>> _testsByName = new();
    private readonly ConcurrentDictionary<string, TestContext> _testContexts = new();
    private readonly TestFactory _testFactory;
    
    private static TestRegistry? _instance;
    private static readonly object _lock = new();
    
    public TestRegistry(TestFactory testFactory)
    {
        _testFactory = testFactory;
    }
    
    /// <summary>
    /// Gets the singleton instance of the test registry
    /// </summary>
    public static TestRegistry Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        throw new InvalidOperationException("TestRegistry has not been initialized. Call Initialize first.");
                    }
                }
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// Initializes the singleton instance
    /// </summary>
    public static void Initialize(TestFactory testFactory)
    {
        lock (_lock)
        {
            _instance = new TestRegistry(testFactory);
        }
    }
    
    /// <summary>
    /// Registers an executable test
    /// </summary>
    public void RegisterTest(ExecutableTest test)
    {
        _allTests[test.TestId] = test;
        
        // Also index by test name for quick lookup
        _testsByName.AddOrUpdate(test.Metadata.TestName,
            new List<ExecutableTest> { test },
            (_, list) =>
            {
                list.Add(test);
                return list;
            });
            
        // Register the test context if it exists
        if (test.Context != null)
        {
            _testContexts[test.TestId] = test.Context;
        }
    }
    
    /// <summary>
    /// Gets all tests matching a predicate
    /// </summary>
    public IEnumerable<TestContext> GetTests(Func<TestContext, bool> predicate)
    {
        return _testContexts.Values.Where(predicate);
    }
    
    /// <summary>
    /// Gets tests by name
    /// </summary>
    public List<TestContext> GetTestsByName(string testName)
    {
        if (_testsByName.TryGetValue(testName, out var tests))
        {
            return tests
                .Where(t => t.Context != null)
                .Select(t => t.Context!)
                .ToList();
        }
        return new List<TestContext>();
    }
    
    /// <summary>
    /// Gets a specific test by ID
    /// </summary>
    public ExecutableTest? GetTest(string testId)
    {
        return _allTests.TryGetValue(testId, out var test) ? test : null;
    }
    
    /// <summary>
    /// Adds a dynamic test during runtime
    /// </summary>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Dynamic test creation may not work with trimming")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Dynamic test creation requires runtime code generation")]
    public async Task<ExecutableTest> AddDynamicTest<T>(TestContext parentContext, DynamicTestInstance<T> dynamicTest) where T : class
    {
        // Create test metadata from the dynamic test
        var metadata = new TestMetadata
        {
            TestId = Guid.NewGuid().ToString(),
            TestName = dynamicTest.TestMethod?.ToString() ?? "DynamicTest",
            TestClassType = typeof(T),
            TestMethodName = "DynamicMethod",
            Categories = [],
            IsSkipped = false,
            SkipReason = null,
            TimeoutMs = null,
            RetryCount = 0,
            CanRunInParallel = true,
            DependsOn = parentContext.Dependencies.ToArray(),
            DataSources = dynamicTest.TestMethodArguments != null && dynamicTest.TestMethodArguments.Length > 0
                ? new[] { new StaticTestDataSource(new[] { dynamicTest.TestMethodArguments }) }
                : [],
            PropertyDataSources = [],
            InstanceFactory = null, // Will use reflection
            TestInvoker = null, // Will use reflection
            ParameterCount = dynamicTest.TestMethodArguments?.Length ?? 0,
            ParameterTypes = dynamicTest.TestMethodArguments?.Select(a => a?.GetType() ?? typeof(object)).ToArray() ?? [],
            Hooks = new TestHooks(),
            MethodInfo = null, // For dynamic tests, we'll need to extract this from the expression
            FilePath = parentContext.TestDetails.TestFilePath,
            LineNumber = parentContext.TestDetails.TestLineNumber
        };
        
        // Create executable test from metadata
        var executableTests = await _testFactory.CreateTests(metadata);
        
        // Register and return the first test (dynamic tests typically create one test)
        foreach (var test in executableTests)
        {
            RegisterTest(test);
            
            // Set up parent-child relationship
            if (parentContext.InternalDiscoveredTest != null)
            {
                var parentTest = GetTest(parentContext.TestDetails.TestId);
                if (parentTest != null)
                {
                    test.Dependencies = new[] { parentTest };
                }
            }
            
            return test;
        }
        
        throw new InvalidOperationException("Failed to create dynamic test");
    }
    
    /// <summary>
    /// Re-registers a test with new arguments
    /// </summary>
    public async Task ReregisterTestWithArguments(TestContext context, object?[]? methodArguments, Dictionary<string, object?>? objectBag)
    {
        var originalTest = GetTest(context.TestDetails.TestId);
        if (originalTest == null)
        {
            throw new InvalidOperationException($"Test {context.TestDetails.TestId} not found in registry");
        }
        
        // Create new metadata with updated arguments
        // Since TestMetadata is not a record, we need to create a new instance
        var updatedMetadata = new TestMetadata
        {
            TestId = Guid.NewGuid().ToString(),
            TestName = originalTest.Metadata.TestName,
            TestClassType = originalTest.Metadata.TestClassType,
            TestMethodName = originalTest.Metadata.TestMethodName,
            Categories = originalTest.Metadata.Categories,
            IsSkipped = originalTest.Metadata.IsSkipped,
            SkipReason = originalTest.Metadata.SkipReason,
            TimeoutMs = originalTest.Metadata.TimeoutMs,
            RetryCount = originalTest.Metadata.RetryCount,
            CanRunInParallel = originalTest.Metadata.CanRunInParallel,
            DependsOn = originalTest.Metadata.DependsOn,
            DataSources = methodArguments != null
                ? new[] { new StaticTestDataSource(new[] { methodArguments }) }
                : originalTest.Metadata.DataSources,
            PropertyDataSources = originalTest.Metadata.PropertyDataSources,
            InstanceFactory = originalTest.Metadata.InstanceFactory,
            TestInvoker = originalTest.Metadata.TestInvoker,
            ParameterCount = methodArguments?.Length ?? originalTest.Metadata.ParameterCount,
            ParameterTypes = methodArguments?.Select(a => a?.GetType() ?? typeof(object)).ToArray() ?? originalTest.Metadata.ParameterTypes,
            Hooks = originalTest.Metadata.Hooks,
            MethodInfo = originalTest.Metadata.MethodInfo,
            FilePath = originalTest.Metadata.FilePath,
            LineNumber = originalTest.Metadata.LineNumber
        };
        
        // Create new executable test
        var newTests = await _testFactory.CreateTests(updatedMetadata);
        
        foreach (var newTest in newTests)
        {
            // Copy dependencies from original
            newTest.Dependencies = originalTest.Dependencies;
            
            // Update the context's object bag if provided
            if (objectBag != null && newTest.Context != null)
            {
                foreach (var kvp in objectBag)
                {
                    newTest.Context.ObjectBag[kvp.Key] = kvp.Value;
                }
            }
            
            // Register the new test
            RegisterTest(newTest);
        }
    }
}