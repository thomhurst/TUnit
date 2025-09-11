using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Services;

/// <summary>
/// Responsible for counter-based test lifecycle management.
/// Follows Single Responsibility Principle - only manages test counts and lifecycle coordination.
/// </summary>
public sealed class TestLifecycleCoordinator : IDisposable
{
    // Counter-based tracking for hook lifecycle
    private readonly ConcurrentDictionary<Type, Counter> _classTestCounts = new();
    private readonly ConcurrentDictionary<Assembly, Counter> _assemblyTestCounts = new();
    private readonly Counter _sessionTestCount = new();
    
    // Track if After hooks have been executed to prevent double-execution
    private readonly ConcurrentDictionary<Type, bool> _afterClassExecuted = new();
    private readonly ConcurrentDictionary<Assembly, bool> _afterAssemblyExecuted = new();
    private volatile bool _afterTestSessionExecuted = false;

    /// <summary>
    /// Initialize counters for all tests before execution begins.
    /// This must be called once with all tests before any test execution.
    /// </summary>
    public void RegisterTests(IEnumerable<AbstractExecutableTest> tests)
    {
        var testList = tests.ToList();
        
        // Initialize session counter
        _sessionTestCount.Add(testList.Count);
        
        // Initialize assembly counters
        foreach (var assemblyGroup in testList.GroupBy(t => t.Metadata.TestClassType.Assembly))
        {
            var counter = _assemblyTestCounts.GetOrAdd(assemblyGroup.Key, _ => new Counter());
            counter.Add(assemblyGroup.Count());
        }
        
        // Initialize class counters
        foreach (var classGroup in testList.GroupBy(t => t.Metadata.TestClassType))
        {
            var counter = _classTestCounts.GetOrAdd(classGroup.Key, _ => new Counter());
            counter.Add(classGroup.Count());
        }
    }

    /// <summary>
    /// Decrement counters and determine which After hooks need to run.
    /// Returns flags indicating which hooks should execute.
    /// </summary>
    public AfterHookExecutionFlags DecrementAndCheckAfterHooks(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass, Assembly testAssembly)
    {
        var flags = new AfterHookExecutionFlags();

        // Decrement class counter and check if we should run After(Class)
        if (_classTestCounts.TryGetValue(testClass, out var classCounter))
        {
            var remainingClassTests = classCounter.Decrement();
            if (remainingClassTests == 0 && _afterClassExecuted.TryAdd(testClass, true))
            {
                flags.ShouldExecuteAfterClass = true;
            }
        }
        
        // Decrement assembly counter and check if we should run After(Assembly)
        if (_assemblyTestCounts.TryGetValue(testAssembly, out var assemblyCounter))
        {
            var remainingAssemblyTests = assemblyCounter.Decrement();
            if (remainingAssemblyTests == 0 && _afterAssemblyExecuted.TryAdd(testAssembly, true))
            {
                flags.ShouldExecuteAfterAssembly = true;
            }
        }
        
        // Decrement session counter and check if we should run After(TestSession)
        var remainingSessionTests = _sessionTestCount.Decrement();
        if (remainingSessionTests == 0 && !_afterTestSessionExecuted)
        {
            _afterTestSessionExecuted = true;
            flags.ShouldExecuteAfterTestSession = true;
        }

        return flags;
    }

    public void Dispose()
    {
        _classTestCounts.Clear();
        _assemblyTestCounts.Clear();
        _afterClassExecuted.Clear();
        _afterAssemblyExecuted.Clear();
    }
}

/// <summary>
/// Flags indicating which After hooks should be executed based on test completion counts.
/// </summary>
public sealed class AfterHookExecutionFlags
{
    public bool ShouldExecuteAfterClass { get; set; }
    public bool ShouldExecuteAfterAssembly { get; set; }
    public bool ShouldExecuteAfterTestSession { get; set; }
}