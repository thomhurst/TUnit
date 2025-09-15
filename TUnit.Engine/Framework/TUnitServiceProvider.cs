using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.Services;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Building;
using TUnit.Engine.Building.Collectors;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Discovery;
using TUnit.Engine.Helpers;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Scheduling;
using TUnit.Engine.Services;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.Engine.Framework;

internal class TUnitServiceProvider : IServiceProvider, IAsyncDisposable
{
    public ITestExecutionFilter? Filter
    {
        get;
    }
    private readonly Dictionary<Type, object> _services = new();

    // Core services
    public TUnitFrameworkLogger Logger { get; }
    public ICommandLineOptions CommandLineOptions { get; }
    public VerbosityService VerbosityService { get; }
    public TestDiscoveryService DiscoveryService { get; }
    public TestBuilderPipeline TestBuilderPipeline { get; }
    public TestSessionCoordinator TestSessionCoordinator { get; }
    public TUnitMessageBus MessageBus { get; }
    public EngineCancellationToken CancellationToken { get; }
    public TestFilterService TestFilterService { get; }
    public IHookCollectionService HookCollectionService { get; }
    public TestExecutor TestExecutor { get; }
    public EventReceiverOrchestrator EventReceiverOrchestrator { get; }
    public ITestFinder TestFinder { get; }
    public TUnitInitializer Initializer { get; }
    public CancellationTokenSource FailFastCancellationSource { get; }
    public ParallelLimitLockProvider ParallelLimitLockProvider { get; }

    public TUnitServiceProvider(IExtension extension,
        ExecuteRequestContext context,
        ITestExecutionFilter? filter,
        IMessageBus messageBus,
        IServiceProvider frameworkServiceProvider,
        ITestFrameworkCapabilities capabilities)
    {
        Filter = filter;
        TestSessionId = context.Request.Session.SessionUid.Value;

        var loggerFactory = frameworkServiceProvider.GetLoggerFactory();
        var outputDevice = frameworkServiceProvider.GetOutputDevice();
        CommandLineOptions = frameworkServiceProvider.GetCommandLineOptions();
        var configuration = frameworkServiceProvider.GetConfiguration();

        TestContext.Configuration = new ConfigurationAdapter(configuration);

        VerbosityService = Register(new VerbosityService(CommandLineOptions));
        DiscoveryDiagnostics.Initialize(VerbosityService);

        Initializer = new TUnitInitializer(CommandLineOptions);

        Logger = Register(new TUnitFrameworkLogger(
            extension,
            outputDevice,
            loggerFactory.CreateLogger<TUnitFrameworkLogger>(),
            VerbosityService));

        // Register the test argument tracking service to handle object disposal for shared instances
        var testArgumentTrackingService = Register(new TestArgumentTrackingService());

        TestFilterService = Register(new TestFilterService(Logger, testArgumentTrackingService));

        MessageBus = Register(new TUnitMessageBus(
            extension,
            CommandLineOptions,
            VerbosityService,
            frameworkServiceProvider,
            context));

        CancellationToken = Register(new EngineCancellationToken());

        EventReceiverOrchestrator = Register(new EventReceiverOrchestrator(Logger));
        HookCollectionService = Register<IHookCollectionService>(new HookCollectionService(EventReceiverOrchestrator));

        ParallelLimitLockProvider = Register(new ParallelLimitLockProvider());

        ContextProvider = Register(new ContextProvider(this, TestSessionId, Filter?.ToString()));

        var hookExecutor = Register(new HookExecutor(HookCollectionService, ContextProvider, EventReceiverOrchestrator));
        var lifecycleCoordinator = Register(new TestLifecycleCoordinator());
        var beforeHookTaskCache = Register(new BeforeHookTaskCache());

        TestExecutor = Register(new TestExecutor(hookExecutor, lifecycleCoordinator, beforeHookTaskCache, ContextProvider, EventReceiverOrchestrator));

        var testExecutionGuard = Register(new TestExecutionGuard());
        var testStateManager = Register(new TestStateManager());
        var testContextRestorer = Register(new TestContextRestorer());
        var testMethodInvoker = Register(new TestMethodInvoker());

        var useSourceGeneration = GetUseSourceGeneration(CommandLineOptions);
#pragma warning disable IL2026 // Using member which has 'RequiresUnreferencedCodeAttribute'
#pragma warning disable IL3050 // Using member which has 'RequiresDynamicCodeAttribute'
        Func<HashSet<Type>?, ITestDataCollector> dataCollectorFactory = filterTypes =>
        {
            if (useSourceGeneration)
            {
                return new AotTestDataCollector(filterTypes);
            }
            else
            {
                return new ReflectionTestDataCollector();
            }
        };
#pragma warning restore IL3050
#pragma warning restore IL2026

        var testBuilder = Register<ITestBuilder>(
            new TestBuilder(TestSessionId, EventReceiverOrchestrator, ContextProvider));

        TestBuilderPipeline = Register(
            new TestBuilderPipeline(
                dataCollectorFactory,
                testBuilder,
                ContextProvider,
                EventReceiverOrchestrator));

        DiscoveryService = Register(new TestDiscoveryService(TestExecutor, TestBuilderPipeline, TestFilterService));

        // Create test finder service after discovery service so it can use its cache
        TestFinder = Register<ITestFinder>(new TestFinder(DiscoveryService));

        var testInitializer = new TestInitializer(EventReceiverOrchestrator);

        // Create the new TestCoordinator that orchestrates the granular services
        var testCoordinator = Register<ITestCoordinator>(
            new TestCoordinator(
                testExecutionGuard,
                testStateManager,
                MessageBus,
                testContextRestorer,
                TestExecutor,
                testInitializer,
                Logger));

        // Create the HookOrchestratingTestExecutorAdapter
        // Note: We'll need to update this to handle dynamic dependencies properly
        var sessionUid = context.Request.Session.SessionUid;
        var isFailFastEnabled = CommandLineOptions.TryGetOptionArgumentList(FailFastCommandProvider.FailFast, out _);
        FailFastCancellationSource = Register(new CancellationTokenSource());

        var testRunner = Register(
            new TestRunner(
                testCoordinator,
                MessageBus,
                isFailFastEnabled,
                FailFastCancellationSource,
                Logger,
                testStateManager));

        // Create scheduler configuration from command line options
        var testGroupingService = Register<ITestGroupingService>(new TestGroupingService());
        var circularDependencyDetector = Register(new CircularDependencyDetector());
        
        var constraintKeyScheduler = Register<IConstraintKeyScheduler>(new ConstraintKeyScheduler(
            testRunner,
            Logger,
            ParallelLimitLockProvider));

        var testScheduler = Register<ITestScheduler>(new TestScheduler(
            Logger,
            testGroupingService,
            MessageBus,
            CommandLineOptions,
            ParallelLimitLockProvider,
            testStateManager,
            testRunner,
            circularDependencyDetector,
            constraintKeyScheduler));

        TestSessionCoordinator = Register(new TestSessionCoordinator(EventReceiverOrchestrator,
            Logger,
            testScheduler,
            serviceProvider: this,
            ContextProvider,
            lifecycleCoordinator,
            MessageBus));

        Register<ITestRegistry>(new TestRegistry(TestBuilderPipeline, testCoordinator, TestSessionId, CancellationToken.Token));

        InitializeConsoleInterceptors();
    }

    public ContextProvider ContextProvider { get; }

    public string TestSessionId { get; }

    private void InitializeConsoleInterceptors()
    {
        var outInterceptor = new StandardOutConsoleInterceptor(VerbosityService);
        var errorInterceptor = new StandardErrorConsoleInterceptor(VerbosityService);

        outInterceptor.Initialize();
        errorInterceptor.Initialize();

        Register(outInterceptor);
        Register(errorInterceptor);
    }

    public object? GetService(Type serviceType)
    {
        return _services.TryGetValue(serviceType, out var service) ? service : null;
    }

    private T Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
        return service;
    }

    private static bool GetUseSourceGeneration(ICommandLineOptions commandLineOptions)
    {
        if (commandLineOptions.TryGetOptionArgumentList(ReflectionModeCommandProvider.ReflectionMode, out _))
        {
            return false; // Reflection mode explicitly requested
        }

        // Check for command line option
        if (commandLineOptions.TryGetOptionArgumentList("tunit-execution-mode", out var modes) && modes.Length > 0)
        {
            var mode = modes[0].ToLowerInvariant();
            if (mode == "sourcegeneration" || mode == "aot")
            {
                return true;
            }
            else if (mode == "reflection")
            {
                return false;
            }
        }

        // Check environment variable
        var envMode = EnvironmentVariableCache.Get("TUNIT_EXECUTION_MODE");
        if (!string.IsNullOrEmpty(envMode))
        {
            var mode = envMode!.ToLowerInvariant();
            if (mode == "sourcegeneration" || mode == "aot")
            {
                return true;
            }
            else if (mode == "reflection")
            {
                return false;
            }
        }

        return SourceRegistrar.IsEnabled;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var service in _services.Values)
        {
            if (service is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (service is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _services.Clear();
    }
}
