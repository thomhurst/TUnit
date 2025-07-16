using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Services;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Engine.Building;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Building.Resolvers;
using TUnit.Engine.Helpers;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;
using IGenericTypeResolver = TUnit.Engine.Building.Interfaces.IGenericTypeResolver;

namespace TUnit.Engine.Framework;

internal class TUnitServiceProvider : IServiceProvider, IAsyncDisposable
{
    private readonly Dictionary<Type, object> _services = new();

    // Core services
    public TUnitFrameworkLogger Logger { get; }
    public ICommandLineOptions CommandLineOptions { get; }
    public VerbosityService VerbosityService { get; }
    public TestDiscoveryService DiscoveryService { get; }
    public UnifiedTestBuilderPipeline TestBuilderPipeline { get; }
    public UnifiedTestExecutor TestExecutor { get; }
    public TUnitMessageBus MessageBus { get; }
    public EngineCancellationToken CancellationToken { get; }
    public TestFilterService TestFilterService { get; }
    public IHookCollectionService HookCollectionService { get; }
    public HookOrchestrator HookOrchestrator { get; }
    public EventReceiverOrchestrator EventReceiverOrchestrator { get; }
    public ITestFinder TestFinder { get; }

    public TUnitServiceProvider(
        IExtension extension,
        ExecuteRequestContext context,
        IMessageBus messageBus,
        IServiceProvider frameworkServiceProvider,
        ITestFrameworkCapabilities capabilities)
    {
        // Get framework services
        var loggerFactory = frameworkServiceProvider.GetLoggerFactory();
        var outputDevice = frameworkServiceProvider.GetOutputDevice();
        CommandLineOptions = frameworkServiceProvider.GetCommandLineOptions();

        // Create core services
        VerbosityService = Register(new VerbosityService(CommandLineOptions));

        // Initialize DiscoveryDiagnostics with VerbosityService
        DiscoveryDiagnostics.Initialize(VerbosityService);

        Logger = Register(new TUnitFrameworkLogger(
            extension,
            outputDevice,
            loggerFactory.CreateLogger<TUnitFrameworkLogger>(),
            VerbosityService));

        TestFilterService = Register(new TestFilterService(Logger));

        MessageBus = Register(new TUnitMessageBus(
            extension,
            CommandLineOptions,
            frameworkServiceProvider,
            context));

        CancellationToken = Register(new EngineCancellationToken());

        // Create test services using unified architecture
        Register<ITestInvoker>(new TestInvoker());

        HookCollectionService = Register<IHookCollectionService>(new HookCollectionService());

        var contextProvider = Register(new ContextProvider());

        HookOrchestrator = Register(new HookOrchestrator(HookCollectionService, Logger, contextProvider, this));
        EventReceiverOrchestrator = Register(new EventReceiverOrchestrator(Logger));

        // Detect execution mode from command line or environment
        var executionMode = GetExecutionMode(CommandLineOptions);

        // Create pipeline dependencies
        var dataCollector = Register<ITestDataCollector>(
            TestDataCollectorFactory.Create(executionMode, assembliesToScan: null));
        var genericResolver = Register<IGenericTypeResolver>(
            CreateGenericTypeResolver(executionMode));
        var testBuilder = Register<ITestBuilder>(
            new TestBuilder(this, contextProvider));

        // Create pipeline with all dependencies
        TestBuilderPipeline = Register(
            new UnifiedTestBuilderPipeline(
                dataCollector,
                genericResolver,
                testBuilder,
                contextProvider));

        DiscoveryService = Register(new TestDiscoveryService(HookOrchestrator, TestBuilderPipeline, TestFilterService));

        // Create test finder service after discovery service so it can use its cache
        TestFinder = Register<ITestFinder>(new TestFinder(DiscoveryService));

        // Create single test executor with ExecutionContext support
        var singleTestExecutor = Register<ISingleTestExecutor>(
            new SingleTestExecutor(Logger, EventReceiverOrchestrator));

        TestExecutor = Register(new UnifiedTestExecutor(
            singleTestExecutor,
            CommandLineOptions,
            Logger,
            loggerFactory,
            testScheduler: null,
            serviceProvider: this));

        // Set session IDs for proper test reporting
        var sessionUid = context.Request.Session.SessionUid;
        singleTestExecutor.SetSessionId(sessionUid);
        TestExecutor.SetSessionId(sessionUid);

        // Initialize console interceptors
        InitializeConsoleInterceptors();
    }

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

    private static TestExecutionMode GetExecutionMode(ICommandLineOptions commandLineOptions)
    {
        if (commandLineOptions.TryGetOptionArgumentList(CommandLineProviders.ReflectionModeCommandProvider.ReflectionMode, out _))
        {
            return TestExecutionMode.Reflection;
        }

        // Check for command line option
        if (commandLineOptions.TryGetOptionArgumentList("tunit-execution-mode", out var modes) && modes.Length > 0)
        {
            if (Enum.TryParse<TestExecutionMode>(modes[0], ignoreCase: true, out var mode))
            {
                return mode;
            }
        }

        // Check environment variable
        var envMode = Environment.GetEnvironmentVariable("TUNIT_EXECUTION_MODE");
        if (!string.IsNullOrEmpty(envMode) &&
            Enum.TryParse<TestExecutionMode>(envMode, ignoreCase: true, out var envModeEnum))
        {
            return envModeEnum;
        }

        // Default to auto-detect based on available tests
        return TestExecutionMode.SourceGeneration;
    }

    private static IGenericTypeResolver CreateGenericTypeResolver(TestExecutionMode executionMode)
    {
        IGenericTypeResolver resolver = executionMode switch
        {
            TestExecutionMode.Reflection => new ReflectionGenericTypeResolver(),
            TestExecutionMode.SourceGeneration => new SourceGeneratedGenericTypeResolver(),
            _ => new SourceGeneratedGenericTypeResolver()
        };
        
        Console.WriteLine($"Creating generic resolver for mode {executionMode}: {resolver.GetType().Name}");
        return resolver;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var service in _services.Values)
        {
            if (service is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (service is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _services.Clear();
    }
}
