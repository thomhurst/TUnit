using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Services;
using TUnit.Engine.Framework;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Scheduling;
using TUnit.Engine.Services;
using ITestExecutor = TUnit.Engine.Interfaces.ITestExecutor;

namespace TUnit.Engine;

internal sealed class TestSessionCoordinator : ITestExecutor, IDisposable, IAsyncDisposable
{
    private readonly ITestOrchestrator _testOrchestrator;
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestScheduler _testScheduler;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TUnitServiceProvider _serviceProvider;
    private readonly Scheduling.TestRunner _testRunner;
    private readonly IContextProvider _contextProvider;
    private readonly ITUnitMessageBus _messageBus;

    public TestSessionCoordinator(
        ITestOrchestrator testOrchestrator,
        ICommandLineOptions commandLineOptions,
        TUnitFrameworkLogger logger,
        ILoggerFactory? loggerFactory,
        ITestScheduler testScheduler,
        TUnitServiceProvider serviceProvider,
        Scheduling.TestRunner testRunner,
        IContextProvider contextProvider,
        ITUnitMessageBus messageBus)
    {
        _testOrchestrator = testOrchestrator;
        _commandLineOptions = commandLineOptions;
        _logger = logger;
        _loggerFactory = loggerFactory ?? new NullLoggerFactory();
        _serviceProvider = serviceProvider;
        _testRunner = testRunner;
        _contextProvider = contextProvider;
        _messageBus = messageBus;
        _testScheduler = testScheduler;
    }

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public async Task ExecuteTests(
        IEnumerable<AbstractExecutableTest> tests,
        ITestExecutionFilter? filter,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        var testList = tests.ToList();

        var testOrchestrator = _serviceProvider.TestExecutor;
        InitializeEventReceivers(testList, cancellationToken);

        try
        {
            await PrepareTestOrchestrator(testOrchestrator, testList, cancellationToken);
            await ExecuteTestsCore(testList, cancellationToken);
        }
        catch (BeforeTestSessionException)
        {
            // Session setup failed - tests have already been marked as failed
        }
        finally
        {
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

    private async Task PrepareTestOrchestrator(TestExecutor testExecutor, List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
    {
        // Register all tests upfront so orchestrator knows total counts per class/assembly for lifecycle management
        testExecutor.RegisterTests(testList);

        await InitializeStaticPropertiesAsync(cancellationToken);
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


    private async Task ExecuteTestsCore(List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
    {
        // Combine cancellation tokens
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _serviceProvider.FailFastCancellationSource.Token);

        // Schedule and execute tests (batch approach to preserve ExecutionContext)
        await _testScheduler.ScheduleAndExecuteAsync(testList, _testRunner, linkedCts.Token);
    }

    private async Task FailAllTestsDueToSessionSetupFailure(List<AbstractExecutableTest> testList, BeforeTestSessionException sessionException)
    {
        // Mark all tests as failed due to session setup failure
        var now = DateTimeOffset.UtcNow;
        foreach (var test in testList)
        {
            test.State = TestState.Failed;
            test.Result = new TestResult
            {
                State = TestState.Failed,
                Exception = sessionException,
                Start = now,
                End = now,
                Duration = TimeSpan.Zero,
                ComputerName = Environment.MachineName
            };

            // Publish test failure to message bus
            await _messageBus.Failed(test.Context, sessionException, now);
        }
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
