using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Services;
using TUnit.Engine.Framework;
using TUnit.Engine.Logging;
using TUnit.Engine.Scheduling;
using TUnit.Engine.Services;
using ITestExecutor = TUnit.Engine.Interfaces.ITestExecutor;

namespace TUnit.Engine;

internal sealed class TestSessionCoordinator : ITestExecutor, IDisposable, IAsyncDisposable
{
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly TUnitFrameworkLogger _logger;
    private readonly ITestScheduler _testScheduler;
    private readonly TUnitServiceProvider _serviceProvider;
    private readonly IContextProvider _contextProvider;
    private readonly TestLifecycleCoordinator _lifecycleCoordinator;
    private readonly ITUnitMessageBus _messageBus;
    private readonly IStaticPropertyInitializer _staticPropertyInitializer;

    public TestSessionCoordinator(EventReceiverOrchestrator eventReceiverOrchestrator,
        TUnitFrameworkLogger logger,
        ITestScheduler testScheduler,
        TUnitServiceProvider serviceProvider,
        IContextProvider contextProvider,
        TestLifecycleCoordinator lifecycleCoordinator,
        ITUnitMessageBus messageBus,
        IStaticPropertyInitializer staticPropertyInitializer)
    {
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _contextProvider = contextProvider;
        _lifecycleCoordinator = lifecycleCoordinator;
        _messageBus = messageBus;
        _testScheduler = testScheduler;
        _staticPropertyInitializer = staticPropertyInitializer;
    }

    public async Task ExecuteTests(
        IEnumerable<AbstractExecutableTest> tests,
        ITestExecutionFilter? filter,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        var testList = tests.ToList();

        InitializeEventReceivers(testList, cancellationToken);

        try
        {
            await PrepareTestOrchestrator(testList, cancellationToken);
            await ExecuteTestsCore(testList, cancellationToken);
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
        var testContexts = testList.Select(static t => t.Context);
        _eventReceiverOrchestrator.InitializeTestCounts(testContexts);
    }

    private async Task PrepareTestOrchestrator(List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
    {
        // Register all tests upfront so orchestrator knows total counts per class/assembly for lifecycle management
        _lifecycleCoordinator.RegisterTests(testList);

        await _staticPropertyInitializer.InitializeAsync(cancellationToken);
    }


    #if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test scheduler uses mode-specific services that handle reflection properly")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test scheduler uses mode-specific services that handle dynamic code properly")]
    #endif
    private async Task ExecuteTestsCore(List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
    {
        // Combine cancellation tokens
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _serviceProvider.FailFastCancellationSource.Token,
            _serviceProvider.CancellationToken.Token);

        // Schedule and execute tests (batch approach to preserve ExecutionContext)
        var success = await _testScheduler.ScheduleAndExecuteAsync(testList, linkedCts.Token);

        // Track whether After(TestSession) hooks failed
        if (!success)
        {
            _serviceProvider.AfterSessionHooksFailed = true;
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
