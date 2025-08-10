using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Services;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Framework;
using TUnit.Engine.Helpers;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Scheduling;
using TUnit.Engine.Services;
using ITestExecutor = TUnit.Engine.Interfaces.ITestExecutor;

namespace TUnit.Engine;

internal sealed class TestExecutor : ITestExecutor, IDisposable, IAsyncDisposable
{
    private readonly ISingleTestExecutor _singleTestExecutor;
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestScheduler _testScheduler;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TUnitServiceProvider _serviceProvider;
    private readonly Scheduling.TestExecutor _testExecutor;
    private readonly IContextProvider _contextProvider;
    private readonly ITUnitMessageBus _messageBus;

    public TestExecutor(
        ISingleTestExecutor singleTestExecutor,
        ICommandLineOptions commandLineOptions,
        TUnitFrameworkLogger logger,
        ILoggerFactory? loggerFactory,
        ITestScheduler? testScheduler,
        TUnitServiceProvider serviceProvider,
        Scheduling.TestExecutor testExecutor,
        IContextProvider contextProvider,
        ITUnitMessageBus messageBus)
    {
        _singleTestExecutor = singleTestExecutor;
        _commandLineOptions = commandLineOptions;
        _logger = logger;
        _loggerFactory = loggerFactory ?? new NullLoggerFactory();
        _serviceProvider = serviceProvider;
        _testExecutor = testExecutor;
        _contextProvider = contextProvider;
        _messageBus = messageBus;

        _testScheduler = testScheduler ?? CreateDefaultScheduler();
    }

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

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

            foreach (var artifact in _contextProvider.TestSessionContext.Artifacts)
            {
                await _messageBus.SessionArtifact(artifact);
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
        // Register all tests upfront so hook orchestrator knows total counts per class/assembly
        hookOrchestrator.RegisterTests(testList);
        
        await InitializeStaticPropertiesAsync(cancellationToken);

        var sessionContext = await hookOrchestrator.ExecuteBeforeTestSessionHooksAsync(cancellationToken);
#if NET
        if (sessionContext != null)
        {
            ExecutionContext.Restore(sessionContext);
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

        // Check environment variables first (can be overridden by command-line)
        if (int.TryParse(EnvironmentVariableCache.Get("TUNIT_ADAPTIVE_MIN_PARALLELISM"), out var envMinParallelism) && envMinParallelism > 0)
        {
            config.AdaptiveMinParallelism = envMinParallelism;
        }

        if (int.TryParse(EnvironmentVariableCache.Get("TUNIT_ADAPTIVE_MAX_PARALLELISM"), out var envMaxParallelism) && envMaxParallelism > 0)
        {
            config.AdaptiveMaxParallelism = envMaxParallelism;
        }

        if (bool.TryParse(EnvironmentVariableCache.Get("TUNIT_ADAPTIVE_METRICS"), out var envMetrics))
        {
            config.EnableAdaptiveMetrics = envMetrics;
        }

        // Handle --maximum-parallel-tests (applies to both fixed and adaptive strategies)
        if (_commandLineOptions.TryGetOptionArgumentList(
            MaximumParallelTestsCommandProvider.MaximumParallelTests,
            out var args) && args.Length > 0)
        {
            if (int.TryParse(args[0], out var maxParallelTests) && maxParallelTests > 0)
            {
                config.MaxParallelism = maxParallelTests;
                config.AdaptiveMaxParallelism = maxParallelTests;
                // Don't change strategy - let it be controlled by --parallelism-strategy
            }
        }

        // Handle --parallelism-strategy
        if (_commandLineOptions.TryGetOptionArgumentList(
            ParallelismStrategyCommandProvider.ParallelismStrategy,
            out var strategyArgs) && strategyArgs.Length > 0)
        {
            var strategy = strategyArgs[0].ToLowerInvariant();
            config.Strategy = strategy == "fixed" ? ParallelismStrategy.Fixed : ParallelismStrategy.Adaptive;
        }

        // Handle --adaptive-metrics
        if (_commandLineOptions.IsOptionSet(AdaptiveMetricsCommandProvider.AdaptiveMetrics))
        {
            config.EnableAdaptiveMetrics = true;
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
        
        // Dispose the scheduler if it implements IDisposable
        (_testScheduler as IDisposable)?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return default(ValueTask);
        }

        _disposed = true;
        
        // Dispose the scheduler if it implements IDisposable
        (_testScheduler as IDisposable)?.Dispose();

        return default(ValueTask);
    }
}
