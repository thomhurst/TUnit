using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Engine.Extensions;
using TUnit.Engine.Logging;

namespace TUnit.Engine;

/// <summary>
/// Simplified test executor that works directly with ExecutableTest
/// </summary>
public sealed class UnifiedTestExecutor : ITestExecutor, IDataProducer
{
    private readonly ISingleTestExecutor _singleTestExecutor;
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly TUnitFrameworkLogger _logger;
    private readonly int _maxParallelism;
    private SessionUid? _sessionUid;
    
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
    
    // IDataProducer implementation
    public string Uid => "TUnit.UnifiedTestExecutor";
    public string Version => "1.0.0";
    public string DisplayName => "TUnit Test Executor";
    public string Description => "Unified test executor for TUnit";
    public Type[] DataTypesProduced => new[] { typeof(TestNodeUpdateMessage) };
    
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);
    
    /// <summary>
    /// Sets the session ID for test reporting
    /// </summary>
    public void SetSessionId(SessionUid sessionUid)
    {
        _sessionUid = sessionUid;
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
            while (remaining.Count > 0)
            {
                var test = remaining.Dequeue();
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
                await _logger.LogErrorAsync("Circular dependency detected in tests. Skipping remaining tests.");
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
        
        // Ensure test context is initialized
        if (test.Context == null)
        {
            test.Context = new TestContext(test.TestId, test.DisplayName);
        }
        
        // Report test started
        await messageBus.PublishAsync(
            this,
            new TestNodeUpdateMessage(
                _sessionUid ?? new Microsoft.Testing.Platform.TestHost.SessionUid(Guid.NewGuid().ToString()),
                test.Context.ToTestNode().WithProperty(InProgressTestNodeStateProperty.CachedInstance)));
        
        try
        {
            // Execute the test
            var updateMessage = await _singleTestExecutor.ExecuteTestAsync(test, messageBus, cancellationToken);
            
            // Publish the result
            await messageBus.PublishAsync(this, updateMessage);
        }
        catch (Exception ex)
        {
            test.State = TestState.Failed;
            test.Result = new TestResult
            {
                Status = Core.Enums.Status.Failed,
                Start = test.StartTime,
                End = DateTimeOffset.Now,
                Duration = DateTimeOffset.Now - test.StartTime.GetValueOrDefault(),
                Exception = ex,
                ComputerName = Environment.MachineName
            };
            
            await messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(
                    _sessionUid ?? new Microsoft.Testing.Platform.TestHost.SessionUid(Guid.NewGuid().ToString()),
                    test.Context.ToTestNode().WithProperty(new FailedTestNodeStateProperty(ex))));
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
            Status = Core.Enums.Status.Skipped,
            Start = test.StartTime ?? DateTimeOffset.Now,
            End = DateTimeOffset.Now,
            Duration = TimeSpan.Zero,
            Exception = null,
            ComputerName = Environment.MachineName,
            OverrideReason = reason
        };
        
        // Ensure test context is initialized
        if (test.Context == null)
        {
            test.Context = new TestContext(test.TestId, test.DisplayName);
        }
        
        await messageBus.PublishAsync(
            this,
            new TestNodeUpdateMessage(
                _sessionUid ?? new Microsoft.Testing.Platform.TestHost.SessionUid(Guid.NewGuid().ToString()),
                test.Context.ToTestNode().WithProperty(new SkippedTestNodeStateProperty(reason))));
    }
    
    private List<ExecutableTest> ApplyFilter(List<ExecutableTest> tests, ITestExecutionFilter filter)
    {
        // Apply filter logic based on filter type
        // This would need to be implemented based on the actual filter requirements
        return tests;
    }
    
    private int GetMaxParallelism()
    {
        // TODO: Get from command line options when API is available
        // For now, default to processor count
        return Environment.ProcessorCount;
    }
    
    /// <summary>
    /// Implementation of ITestExecutor for Microsoft.Testing.Platform
    /// </summary>
    public async Task ExecuteAsync(
        RunTestExecutionRequest request,
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