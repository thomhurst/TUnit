using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.OutputDevice;
using Microsoft.Testing.Platform.Services;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine.Framework;

internal class TUnitServiceProvider : IServiceProvider, IAsyncDisposable
{
    private readonly Dictionary<Type, object> _services = [];

    public ILoggerFactory LoggerFactory;
    public IOutputDevice OutputDevice;
    public ICommandLineOptions CommandLineOptions;

    public TUnitFrameworkLogger Logger { get; }
    public TUnitMessageBus TUnitMessageBus { get; }

    public TUnitInitializer Initializer { get; }
    public StandardOutConsoleInterceptor StandardOutConsoleInterceptor { get; }
    public StandardErrorConsoleInterceptor StandardErrorConsoleInterceptor { get; }
    public TUnitTestDiscoverer TestDiscoverer { get; }
    public TestGrouper TestGrouper { get; }
    public TestsFinder TestFinder { get; }
    public TestsExecutor TestsExecutor { get; }
    public OnEndExecutor OnEndExecutor { get; }
    public FilterParser FilterParser { get; }
    public TestDiscoveryHookOrchestrator TestDiscoveryHookOrchestrator { get; }
    public TestSessionHookOrchestrator TestSessionHookOrchestrator { get; }
    public AssemblyHookOrchestrator AssemblyHookOrchestrator { get; }

    public TUnitServiceProvider(IExtension extension,
        ExecuteRequestContext context,
        IMessageBus messageBus,
        IServiceProvider frameworkServiceProvider)
    {
        LoggerFactory = Register(frameworkServiceProvider.GetLoggerFactory());
        
        OutputDevice = Register(frameworkServiceProvider.GetOutputDevice());
        
        CommandLineOptions = Register(frameworkServiceProvider.GetCommandLineOptions());

        Logger = Register(new TUnitFrameworkLogger(extension, OutputDevice, LoggerFactory.CreateLogger<TUnitFrameworkLogger>()));
        
        Initializer = Register(new TUnitInitializer(CommandLineOptions));
        
        StandardOutConsoleInterceptor = Register(new StandardOutConsoleInterceptor(CommandLineOptions));
        
        StandardErrorConsoleInterceptor = Register(new StandardErrorConsoleInterceptor(CommandLineOptions));

        FilterParser = Register(new FilterParser());

        var stringFilter = FilterParser.GetTestFilter(context);

        TUnitMessageBus = Register(new TUnitMessageBus(extension, context));
        
        var hooksCollector = Register(new HooksCollector());
        
        var testMetadataCollector = Register(new TestMetadataCollector(TUnitMessageBus, LoggerFactory));
        var testsLoader = Register(new TestsConstructor(extension, testMetadataCollector, this));
        var testFilterService = Register(new TestFilterService(LoggerFactory));
        
        TestGrouper = Register(new TestGrouper());
        
        AssemblyHookOrchestrator = Register(new AssemblyHookOrchestrator(hooksCollector));

        TestDiscoveryHookOrchestrator = Register(new TestDiscoveryHookOrchestrator(hooksCollector, stringFilter));
        TestSessionHookOrchestrator = Register(new TestSessionHookOrchestrator(hooksCollector, AssemblyHookOrchestrator, stringFilter));
        
        var classHookOrchestrator = Register(new ClassHookOrchestrator(hooksCollector));
        
        var testHookOrchestrator = Register(new TestHookOrchestrator(hooksCollector));

        var testRegistrar = Register(new TestRegistrar(AssemblyHookOrchestrator, classHookOrchestrator));
        TestDiscoverer = Register(new TUnitTestDiscoverer(hooksCollector, testsLoader, testFilterService, TestGrouper, testRegistrar, TestDiscoveryHookOrchestrator, TUnitMessageBus, LoggerFactory, extension));
        
        TestFinder = Register(new TestsFinder(TestDiscoverer));
        
        var disposer = Register(new Disposer(Logger));
        var cancellationTokenSource = Register(EngineCancellationToken.CancellationTokenSource);
        var testInvoker = Register(new TestInvoker(testHookOrchestrator));
        var explicitFilterService = Register(new ExplicitFilterService());
        var parallelLimitProvider = Register(new ParallelLimitProvider());
        var hookMessagePublisher = Register(new HookMessagePublisher(extension, messageBus));
        
        var singleTestExecutor = Register(new SingleTestExecutor(extension, disposer, cancellationTokenSource, testInvoker,
            explicitFilterService, parallelLimitProvider, AssemblyHookOrchestrator, classHookOrchestrator, testHookOrchestrator, TestFinder, TUnitMessageBus, Logger));
        
        TestsExecutor = Register(new TestsExecutor(singleTestExecutor, Logger, CommandLineOptions));
        
        OnEndExecutor = Register(new OnEndExecutor(CommandLineOptions, Logger));
    }
    
    public async ValueTask DisposeAsync()
    {
        await StandardOutConsoleInterceptor.DisposeAsync();
        await StandardErrorConsoleInterceptor.DisposeAsync();
    }

    private T Register<T>(T t)
    {
        _services.Add(t!.GetType(), t);
        return t;
    }

    public object? GetService(Type serviceType)
    {
        return _services.GetValueOrDefault(serviceType);
    }
}