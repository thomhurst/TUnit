using System.Diagnostics.CodeAnalysis;
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

        TUnitMessageBus = Register(new TUnitMessageBus(extension, context));
        
        var testMetadataCollector = Register(new TestMetadataCollector(TUnitMessageBus, LoggerFactory));
        var testsLoader = Register(new TestsConstructor(extension, testMetadataCollector, this));
        var testFilterService = Register(new TestFilterService(LoggerFactory));
        
        TestGrouper = Register(new TestGrouper());

        TestDiscoverer = Register(new TUnitTestDiscoverer(testsLoader, testFilterService, TestGrouper, TUnitMessageBus, LoggerFactory, extension));
        
        TestFinder = Register(new TestsFinder(TestDiscoverer, TUnitMessageBus));
        
        var disposer = Register(new Disposer(Logger));
        var cancellationTokenSource = Register(EngineCancellationToken.CancellationTokenSource);
        var testInvoker = Register(new TestInvoker());
        var explicitFilterService = Register(new ExplicitFilterService());
        var parallelLimitProvider = Register(new ParallelLimitProvider());
        var hookMessagePublisher = Register(new HookMessagePublisher(extension, messageBus));
        var globalStaticTestHookOrchestrator = Register(new GlobalStaticTestHookOrchestrator(hookMessagePublisher));
        var assemblyHookOrchestrator =
            Register(new AssemblyHookOrchestrator(hookMessagePublisher, globalStaticTestHookOrchestrator));
        var classHookOrchestrator = Register(new ClassHookOrchestrator(hookMessagePublisher, globalStaticTestHookOrchestrator));
        var singleTestExecutor = Register(new SingleTestExecutor(extension, disposer, cancellationTokenSource, testInvoker,
            explicitFilterService, parallelLimitProvider, assemblyHookOrchestrator, classHookOrchestrator, TestFinder, Logger));
        
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