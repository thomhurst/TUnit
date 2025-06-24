using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Logging;

namespace TUnit.Engine;

/// <summary>
/// Simplified test executor that works directly with ExecutableTest
/// </summary>
public sealed class UnifiedTestExecutor : ITestExecutor
{
    private readonly ISingleTestExecutor _singleTestExecutor;
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly TUnitFrameworkLogger _logger;
    private readonly int _maxParallelism;
    
    public UnifiedTestExecutor(
        ISingleTestExecutor singleTestExecutor,
        ICommandLineOptions commandLineOptions,
        TUnitFrameworkLogger logger)
    {
        _singleTestExecutor = singleTestExecutor;
        _commandLineOptions = commandLineOptions;
        _logger = logger;
        _maxParallelism = GetMaxParallelism();
    }
    
    /// <summary>
    /// Executes a collection of tests with proper parallelization and dependency handling
    /// </summary>
    public async Task ExecuteTests(
        IEnumerable<ExecutableTest> tests,
        ITestExecutionFilter? filter,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        var testList = tests.ToList();
        
        // Apply filter if provided
        if (filter != null)
        {
            testList = ApplyFilter(testList, filter);
        }
        
        // Group tests by parallelization capability
        var parallelTests = testList.Where(t => t.Metadata.CanRunInParallel && !t.Dependencies.Any()).ToList();
        var serialTests = testList.Where(t => !t.Metadata.CanRunInParallel || t.Dependencies.Any()).ToList();
        
        // Execute parallel tests
        await ExecuteParallelTests(parallelTests, messageBus, cancellationToken);
        
        // Execute serial tests (respecting dependencies)
        await ExecuteSerialTests(serialTests, messageBus, cancellationToken);
    }
    
    private async Task ExecuteParallelTests(
        List<ExecutableTest> tests,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        if (!tests.Any()) return;
        
        using var semaphore = new SemaphoreSlim(_maxParallelism, _maxParallelism);
        var tasks = new List<Task>();
        
        foreach (var test in tests)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            await semaphore.WaitAsync(cancellationToken);
            
            var task = Task.Run(async () =>
            {
                try
                {
                    await ExecuteSingleTest(test, messageBus, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);
            
            tasks.Add(task);
        }
        
        await Task.WhenAll(tasks);
    }
    
    private async Task ExecuteSerialTests(
        List<ExecutableTest> tests,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        if (!tests.Any()) return;
        
        // Build dependency graph
        var completed = new HashSet<string>();
        var remaining = new Queue<ExecutableTest>(tests.Where(t => !t.Dependencies.Any()));
        var waiting = tests.Where(t => t.Dependencies.Any()).ToList();
        
        while (remaining.Count > 0 || waiting.Count > 0)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            
            // Execute tests with satisfied dependencies
            while (remaining.TryDequeue(out var test))
            {
                await ExecuteSingleTest(test, messageBus, cancellationToken);
                completed.Add(test.TestId);
            }
            
            // Check for newly runnable tests
            var newlyRunnable = waiting
                .Where(t => t.Dependencies.All(d => completed.Contains(d.TestId)))
                .ToList();
            
            foreach (var test in newlyRunnable)
            {
                remaining.Enqueue(test);
                waiting.Remove(test);
            }
            
            // If no progress can be made, we have a circular dependency
            if (remaining.Count == 0 && waiting.Count > 0)
            {
                _logger.LogError("Circular dependency detected in tests. Skipping remaining tests.");
                foreach (var test in waiting)
                {
                    await ReportTestSkipped(test, "Circular dependency detected", messageBus);
                }
                break;
            }
        }
    }
    
    private async Task ExecuteSingleTest(
        ExecutableTest test,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        test.State = TestState.Running;
        test.StartTime = DateTimeOffset.UtcNow;
        
        // Report test started
        await messageBus.PublishAsync(
            this,
            new TestNodeUpdateMessage(
                test.TestId,
                new TestNodeStateProperty(TestNodeState.Running)));
        
        try
        {
            // Execute the test
            await _singleTestExecutor.ExecuteTest(test, cancellationToken);
            
            // Report result based on test state
            var testNodeState = test.State switch
            {
                TestState.Passed => TestNodeState.Passed,
                TestState.Failed => TestNodeState.Failed,
                TestState.Skipped => TestNodeState.Skipped,
                TestState.Timeout => TestNodeState.Timeout,
                _ => TestNodeState.Failed
            };
            
            var properties = new PropertyBag();
            if (test.Result?.Exception != null)
            {
                properties.Add(new FailedTestNodeStateProperty(test.Result.Exception));
            }
            
            if (test.Duration.HasValue)
            {
                properties.Add(new DurationProperty(test.Duration.Value));
            }
            
            await messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(
                    test.TestId,
                    new TestNodeStateProperty(testNodeState, properties)));
        }
        catch (Exception ex)
        {
            test.State = TestState.Failed;
            test.Result = new TestResult
            {
                State = TestState.Failed,
                Exception = ex,
                Message = $"Test execution failed: {ex.Message}"
            };
            
            await messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(
                    test.TestId,
                    new TestNodeStateProperty(
                        TestNodeState.Failed,
                        new PropertyBag(new FailedTestNodeStateProperty(ex)))));
        }
        finally
        {
            test.EndTime = DateTimeOffset.UtcNow;
        }
    }
    
    private async Task ReportTestSkipped(
        ExecutableTest test,
        string reason,
        IMessageBus messageBus)
    {
        test.State = TestState.Skipped;
        test.Result = new TestResult
        {
            State = TestState.Skipped,
            Message = reason
        };
        
        await messageBus.PublishAsync(
            this,
            new TestNodeUpdateMessage(
                test.TestId,
                new TestNodeStateProperty(
                    TestNodeState.Skipped,
                    new PropertyBag(new SkippedTestNodeStateProperty(reason)))));
    }
    
    private List<ExecutableTest> ApplyFilter(List<ExecutableTest> tests, ITestExecutionFilter filter)
    {
        // Apply filter logic based on filter type
        // This would need to be implemented based on the actual filter requirements
        return tests;
    }
    
    private int GetMaxParallelism()
    {
        // Get from command line options or default to processor count
        var maxParallelismOption = _commandLineOptions.GetOptionValue("maximum-parallel-tests");
        if (int.TryParse(maxParallelismOption, out var max) && max > 0)
        {
            return max;
        }
        
        return Environment.ProcessorCount;
    }
    
    /// <summary>
    /// Implementation of ITestExecutor for Microsoft.Testing.Platform
    /// </summary>
    public async Task ExecuteAsync(
        ExecuteTestsExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        // Get tests from discovery
        var discoveryService = new TestDiscoveryService(
            TestMetadataRegistry.GetSources(),
            new TestFactory(
                new DefaultTestInvoker(),
                new DefaultHookInvoker(),
                new DefaultDataSourceResolver()),
            enableDynamicDiscovery: false);
        
        var tests = await discoveryService.DiscoverTests();
        
        // Execute tests
        await ExecuteTests(tests, request.Filter, messageBus, cancellationToken);
    }
}

/// <summary>
/// Executes a single test
/// </summary>
public interface ISingleTestExecutor
{
    Task ExecuteTest(ExecutableTest test, CancellationToken cancellationToken);
}

/// <summary>
/// Default implementation of single test executor
/// </summary>
public sealed class DefaultSingleTestExecutor : ISingleTestExecutor
{
    private readonly TUnitFrameworkLogger _logger;
    
    public DefaultSingleTestExecutor(TUnitFrameworkLogger logger)
    {
        _logger = logger;
    }
    
    public async Task ExecuteTest(ExecutableTest test, CancellationToken cancellationToken)
    {
        object? instance = null;
        
        try
        {
            // Skip if requested
            if (test.Metadata.IsSkipped)
            {
                test.State = TestState.Skipped;
                test.Result = new TestResult
                {
                    State = TestState.Skipped,
                    Message = test.Metadata.SkipReason ?? "Test skipped"
                };
                return;
            }
            
            // Create context
            test.Context = new TestContext(test.Metadata.TestName, test.DisplayName);
            
            // Run before class hooks
            var hookContext = new HookContext(test.Context, test.Metadata.TestClassType, null);
            foreach (var hook in test.Hooks.BeforeClass)
            {
                await hook(hookContext);
            }
            
            // Create instance
            instance = await test.CreateInstance();
            
            // Run after class hooks
            hookContext = new HookContext(test.Context, test.Metadata.TestClassType, instance);
            foreach (var hook in test.Hooks.AfterClass)
            {
                await hook(instance, hookContext);
            }
            
            // Run before test hooks
            foreach (var hook in test.Hooks.BeforeTest)
            {
                await hook(instance, hookContext);
            }
            
            // Execute test with timeout if specified
            if (test.Metadata.TimeoutMs.HasValue)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(test.Metadata.TimeoutMs.Value);
                
                try
                {
                    await test.InvokeTest(instance).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"Test exceeded timeout of {test.Metadata.TimeoutMs}ms");
                }
            }
            else
            {
                await test.InvokeTest(instance).ConfigureAwait(false);
            }
            
            // Run after test hooks
            foreach (var hook in test.Hooks.AfterTest)
            {
                await hook(instance, hookContext);
            }
            
            // Success
            test.State = TestState.Passed;
            test.Result = new TestResult
            {
                State = TestState.Passed,
                Output = test.Context.GetOutput()
            };
        }
        catch (Exception ex)
        {
            test.State = TestState.Failed;
            test.Result = new TestResult
            {
                State = TestState.Failed,
                Exception = ex,
                Message = ex.Message,
                Output = test.Context?.GetOutput()
            };
            
            _logger.LogError($"Test {test.DisplayName} failed: {ex}");
        }
        finally
        {
            // Dispose if necessary
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else if (instance is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
        }
    }
}

// Default implementations for dependencies
public sealed class DefaultTestInvoker : ITestInvoker
{
    public async Task InvokeTestMethod(object instance, MethodInfo method, object?[] arguments)
    {
        var result = method.Invoke(instance, arguments);
        if (result is Task task)
        {
            await task;
        }
    }
}

public sealed class DefaultHookInvoker : IHookInvoker
{
    public async Task InvokeHook(HookMetadata hook, HookContext context)
    {
        if (hook.MethodInfo == null) return;
        
        var instance = hook.IsStatic ? null : context.TestInstance;
        var result = hook.MethodInfo.Invoke(instance, new object[] { context });
        
        if (result is Task task)
        {
            await task;
        }
    }
}

public sealed class DefaultDataSourceResolver : IDataSourceResolver
{
    public async Task<IEnumerable<object?[]>> ResolveDataSource(TestDataSource dataSource)
    {
        if (dataSource is StaticTestDataSource staticSource)
        {
            return staticSource.GetData();
        }
        
        // Dynamic sources would need reflection to resolve
        return Array.Empty<object?[]>();
    }
}