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

internal sealed class TestExecutor : ITestExecutor, IDataProducer, IDisposable, IAsyncDisposable
{
    private readonly ISingleTestExecutor _singleTestExecutor;
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestScheduler _testScheduler;
    private readonly ILoggerFactory _loggerFactory;
    private SessionUid? _sessionUid;
    private readonly TUnitServiceProvider _serviceProvider;
    private readonly Scheduling.TestExecutor _testExecutor;

    public TestExecutor(
        ISingleTestExecutor singleTestExecutor,
        ICommandLineOptions commandLineOptions,
        TUnitFrameworkLogger logger,
        ILoggerFactory? loggerFactory,
        ITestScheduler? testScheduler,
        TUnitServiceProvider serviceProvider,
        Scheduling.TestExecutor testExecutor)
    {
        _singleTestExecutor = singleTestExecutor;
        _commandLineOptions = commandLineOptions;
        _logger = logger;
        _loggerFactory = loggerFactory ?? new NullLoggerFactory();
        _serviceProvider = serviceProvider;
        _testExecutor = testExecutor;

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

    public void SetSessionId(SessionUid sessionUid)
    {
        _sessionUid = sessionUid;
    }

    public async Task ExecuteTests(
        IEnumerable<AbstractExecutableTest> tests,
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
            await ExecuteTestsCore(testList, _testExecutor, cancellationToken);
        }
        finally
        {
            // Execute session cleanup hooks with a separate cancellation token to ensure
            // cleanup executes even when test execution is cancelled
            try
            {
                using var cleanupCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                var afterSessionContext = await hookOrchestrator.ExecuteAfterTestSessionHooksAsync(cleanupCts.Token);
#if NET
                if (afterSessionContext != null)
                {
                    ExecutionContext.Restore(afterSessionContext);
                }
#endif
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in session cleanup hooks: {ex}");
            }
        }
    }

    private void InitializeEventReceivers(List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
    {
        if (_serviceProvider.GetService(typeof(EventReceiverOrchestrator)) is not EventReceiverOrchestrator eventReceiverOrchestrator)
        {
            return;
        }

        var testContexts = testList.Select(t => t.Context);
        eventReceiverOrchestrator.InitializeTestCounts(testContexts);

        // Test registered event receivers are now invoked during discovery phase
    }

    private async Task PrepareHookOrchestrator(HookOrchestrator hookOrchestrator, List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
    {
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
            // Execute all registered global initializers (including static property initialization from source generation)
            while (Sources.GlobalInitializers.TryDequeue(out var initializer))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await initializer();
            }

            // For reflection mode, also initialize static properties dynamically
            if (!SourceRegistrar.IsEnabled)
            {
                await StaticPropertyReflectionInitializer.InitializeAllStaticPropertiesAsync();
            }
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Error during static property initialization: {ex}");
            throw;
        }
    }


    private async Task ExecuteTestsCore(List<AbstractExecutableTest> testList, Scheduling.ITestExecutor executorAdapter, CancellationToken cancellationToken)
    {
        // Combine cancellation tokens
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _serviceProvider.FailFastCancellationSource.Token);

        // Schedule and execute tests (batch approach to preserve ExecutionContext)
        await _testScheduler.ScheduleAndExecuteAsync(testList, executorAdapter, linkedCts.Token);
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

        var eventReceiverOrchestrator = _serviceProvider.GetService(typeof(EventReceiverOrchestrator)) as EventReceiverOrchestrator;
        var hookOrchestrator = _serviceProvider.GetService(typeof(HookOrchestrator)) as HookOrchestrator;
        return TestSchedulerFactory.Create(config, _logger, _serviceProvider.MessageBus, _serviceProvider.CancellationToken, eventReceiverOrchestrator!, hookOrchestrator!);
    }


    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return default(ValueTask);
        }

        _disposed = true;

        return default(ValueTask);
    }
}
