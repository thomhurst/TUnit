using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Framework;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Scheduling;
using TUnit.Engine.Services;
using ITestExecutor = TUnit.Engine.Interfaces.ITestExecutor;

namespace TUnit.Engine;

/// <summary>
/// Simplified test executor that works directly with ExecutableTest
/// </summary>
internal sealed class UnifiedTestExecutor : ITestExecutor, IDataProducer, IDisposable, IAsyncDisposable
{
    private readonly ISingleTestExecutor _singleTestExecutor;
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestScheduler _testScheduler;
    private readonly ILoggerFactory _loggerFactory;
    private SessionUid? _sessionUid;
    private readonly CancellationTokenSource _failFastCancellationSource = new();
    private readonly TUnitServiceProvider _serviceProvider;

    public UnifiedTestExecutor(
        ISingleTestExecutor singleTestExecutor,
        ICommandLineOptions commandLineOptions,
        TUnitFrameworkLogger logger,
        ILoggerFactory? loggerFactory,
        ITestScheduler? testScheduler,
        TUnitServiceProvider serviceProvider)
    {
        _singleTestExecutor = singleTestExecutor;
        _commandLineOptions = commandLineOptions;
        _logger = logger;
        _loggerFactory = loggerFactory ?? new NullLoggerFactory();
        _serviceProvider = serviceProvider;

        // Use provided scheduler or create default
        _testScheduler = testScheduler ?? CreateDefaultScheduler();
    }

    // IDataProducer implementation
    public string Uid => "TUnit.UnifiedTestExecutor";
    public string Version => "1.0.0";
    public string DisplayName => "TUnit Test Executor";
    public string Description => "Unified test executor for TUnit";
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

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

        var hookOrchestrator = _serviceProvider.HookOrchestrator;
        InitializeEventReceivers(testList, cancellationToken);

        try
        {
            await PrepareHookOrchestrator(hookOrchestrator, testList, cancellationToken);
            var executorAdapter = CreateExecutorAdapter(hookOrchestrator, messageBus);
            await ExecuteTestsCore(testList, executorAdapter, cancellationToken);
        }
        finally
        {
            var afterSessionContext = await hookOrchestrator.ExecuteAfterTestSessionHooksAsync(cancellationToken);
#if NET
            if (afterSessionContext != null)
            {
                ExecutionContext.Restore(afterSessionContext);
            }
#endif
        }
    }

    private void InitializeEventReceivers(List<ExecutableTest> testList, CancellationToken cancellationToken)
    {
        var eventReceiverOrchestrator = _serviceProvider.GetService(typeof(EventReceiverOrchestrator)) as EventReceiverOrchestrator;
        if (eventReceiverOrchestrator == null)
        {
            return;
        }

        // Initialize test counts for first/last event receivers
        var testContexts = testList.Select(t => t.Context);
        eventReceiverOrchestrator.InitializeTestCounts(testContexts);

        // Test registered event receivers are now invoked during discovery phase
    }

    private async Task PrepareHookOrchestrator(HookOrchestrator hookOrchestrator, List<ExecutableTest> testList, CancellationToken cancellationToken)
    {
        // Initialize static properties with data source attributes before any other session setup
        await InitializeStaticPropertiesAsync(cancellationToken);

        var beforeSessionContext = await hookOrchestrator.ExecuteBeforeTestSessionHooksAsync(cancellationToken);
#if NET
        if (beforeSessionContext != null)
        {
            ExecutionContext.Restore(beforeSessionContext);
        }
#endif
    }

    private async Task InitializeStaticPropertiesAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Execute all registered global initializers (including static property initialization)
            while (Sources.GlobalInitializers.TryDequeue(out var initializer))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await initializer();
            }
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Error during static property initialization: {ex}");
            throw;
        }
    }

    private Scheduling.ITestExecutor CreateExecutorAdapter(HookOrchestrator hookOrchestrator, IMessageBus messageBus)
    {
        var isFailFastEnabled = IsFailFastEnabled();
        var sessionUid = _sessionUid ?? new SessionUid(Guid.NewGuid().ToString());

        return new HookOrchestratingTestExecutorAdapter(
            _singleTestExecutor,
            messageBus,
            sessionUid,
            isFailFastEnabled,
            _failFastCancellationSource,
            _logger,
            hookOrchestrator);
    }

    private async Task ExecuteTestsCore(List<ExecutableTest> testList, Scheduling.ITestExecutor executorAdapter, CancellationToken cancellationToken)
    {
        // Combine cancellation tokens
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _failFastCancellationSource.Token);

        // Schedule and execute tests (batch approach to preserve ExecutionContext)
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
        var config = SchedulerConfiguration.Default;

        if (_commandLineOptions.TryGetOptionArgumentList(
            MaximumParallelTestsCommandProvider.MaximumParallelTests,
            out var args) && args.Length > 0)
        {
            if (int.TryParse(args[0], out var maxParallelTests) && maxParallelTests > 0)
            {
                config.MaxParallelism = maxParallelTests;
            }
        }

        return TestSchedulerFactory.Create(config, _logger, _serviceProvider.CancellationToken);
    }


    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _failFastCancellationSource.Dispose();
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return default(ValueTask);
        }

        _failFastCancellationSource.Dispose();
        _disposed = true;

        return default(ValueTask);
    }
}
