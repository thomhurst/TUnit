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
using TUnit.Engine.Scheduling;
using TUnit.Engine.CommandLineProviders;

namespace TUnit.Engine;

/// <summary>
/// Simplified test executor that works directly with ExecutableTest
/// </summary>
public sealed class UnifiedTestExecutor : ITestExecutor, IDataProducer
{
    private readonly ISingleTestExecutor _singleTestExecutor;
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestScheduler _testScheduler;
    private SessionUid? _sessionUid;
    private readonly CancellationTokenSource _failFastCancellationSource = new();
    
    public UnifiedTestExecutor(
        ISingleTestExecutor singleTestExecutor,
        ICommandLineOptions commandLineOptions,
        TUnitFrameworkLogger logger,
        ITestScheduler? testScheduler = null)
    {
        _singleTestExecutor = singleTestExecutor;
        _commandLineOptions = commandLineOptions;
        _logger = logger;
        
        // Use provided scheduler or create default
        _testScheduler = testScheduler ?? CreateDefaultScheduler();
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
        
        // Check if fail-fast is enabled
        var isFailFastEnabled = IsFailFastEnabled();
        
        // Create executor adapter with fail-fast support
        var executorAdapter = new FailFastTestExecutorAdapter(
            _singleTestExecutor,
            messageBus,
            _sessionUid ?? new SessionUid(Guid.NewGuid().ToString()),
            isFailFastEnabled,
            _failFastCancellationSource,
            _logger);
        
        // Combine cancellation tokens
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, 
            _failFastCancellationSource.Token);
        
        // Schedule and execute tests
        await _testScheduler.ScheduleAndExecuteAsync(testList, executorAdapter, linkedCts.Token);
    }
    
    private bool IsFailFastEnabled()
    {
        return _commandLineOptions.TryGetOptionArgumentList(
            FailFastCommandProvider.FailFast, 
            out _);
    }
    
    private ITestScheduler CreateDefaultScheduler()
    {
        return TestSchedulerFactory.CreateDefault(_logger);
    }
    
    private List<ExecutableTest> ApplyFilter(List<ExecutableTest> tests, ITestExecutionFilter filter)
    {
        // Apply filter logic based on filter type
        // This would need to be implemented based on the actual filter requirements
        return tests;
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
                new TestInvoker(),
                new HookInvoker(),
                new DataSourceResolver()),
            enableDynamicDiscovery: false);
        
        var tests = await discoveryService.DiscoverTests();
        
        // Execute tests
        await ExecuteTests(tests, request.Filter, messageBus, cancellationToken);
    }
}