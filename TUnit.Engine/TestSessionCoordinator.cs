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

    public TestSessionCoordinator(EventReceiverOrchestrator eventReceiverOrchestrator,
        TUnitFrameworkLogger logger,
        ITestScheduler testScheduler,
        TUnitServiceProvider serviceProvider,
        IContextProvider contextProvider,
        TestLifecycleCoordinator lifecycleCoordinator,
        ITUnitMessageBus messageBus)
    {
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _contextProvider = contextProvider;
        _lifecycleCoordinator = lifecycleCoordinator;
        _messageBus = messageBus;
        _testScheduler = testScheduler;
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Static property initialization uses reflection in reflection mode")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Data source initialization may require dynamic code generation")]
#pragma warning disable IL2046, IL3051 // Interface implementation - cannot add attributes to match called method requirements
    #endif
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
    #if NET6_0_OR_GREATER
#pragma warning restore IL2046, IL3051
    #endif

    private void InitializeEventReceivers(List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
    {
        var testContexts = testList.Select(t => t.Context);
        _eventReceiverOrchestrator.InitializeTestCounts(testContexts);
    }

    private async Task PrepareTestOrchestrator(List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
    {
        // Register all tests upfront so orchestrator knows total counts per class/assembly for lifecycle management
        _lifecycleCoordinator.RegisterTests(testList);

        #if NET6_0_OR_GREATER
        #pragma warning disable IL2026, IL3050 // Reflection only used when !SourceRegistrar.IsEnabled
        #endif
        await InitializeStaticPropertiesAsync(cancellationToken);
        #if NET6_0_OR_GREATER
        #pragma warning restore IL2026, IL3050
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
                #if NET6_0_OR_GREATER
                #pragma warning disable IL2026, IL3050 // Reflection only used in reflection mode, not in AOT/source-gen mode
                #endif
                await StaticPropertyReflectionInitializer.InitializeAllStaticPropertiesAsync();
                #if NET6_0_OR_GREATER
                #pragma warning restore IL2026, IL3050
                #endif
            }
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Error during static property initialization: {ex}");
            throw;
        }
    }


    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteTestsCore(List<AbstractExecutableTest> testList, CancellationToken cancellationToken)
    {
        // Combine cancellation tokens
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _serviceProvider.FailFastCancellationSource.Token);

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
