using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
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

internal class TUnitServiceProvider : IAsyncDisposable
{
    public ILoggerFactory LoggerFactory;
    public IOutputDevice OutputDevice;
    public ICommandLineOptions CommandLineOptions;

    public TUnitFrameworkLogger Logger { get; }
    public TUnitInitializer Initializer { get; }
    public StandardOutConsoleInterceptor StandardOutConsoleInterceptor { get; }
    public StandardErrorConsoleInterceptor StandardErrorConsoleInterceptor { get; }
    public TUnitTestDiscoverer TestDiscoverer { get; }
    public TestGrouper TestGrouper { get; }
    public TestsExecutor TestsExecutor { get; }
    public OnEndExecutor OnEndExecutor { get; }

    public TUnitServiceProvider(IExtension extension,
        IMessageBus messageBus,
        IServiceProvider frameworkServiceProvider)
    {
        LoggerFactory = frameworkServiceProvider.GetLoggerFactory();
        
        OutputDevice = frameworkServiceProvider.GetOutputDevice();
        
        CommandLineOptions = frameworkServiceProvider.GetCommandLineOptions();

        Logger = new TUnitFrameworkLogger(extension, OutputDevice, LoggerFactory.CreateLogger<TUnitFrameworkLogger>());
        
        Initializer = new TUnitInitializer(CommandLineOptions);
        
        StandardOutConsoleInterceptor = new StandardOutConsoleInterceptor(CommandLineOptions);
        
        StandardErrorConsoleInterceptor = new StandardErrorConsoleInterceptor(CommandLineOptions);
        
        var testsLoader = new TestsLoader(LoggerFactory);
        var testFilterService = new TestFilterService(LoggerFactory);
        
        TestDiscoverer = new TUnitTestDiscoverer(testsLoader, testFilterService, LoggerFactory);
       
        TestGrouper = new TestGrouper();
        var disposer = new Disposer(Logger);
        var cancellationTokenSource = EngineCancellationToken.CancellationTokenSource;
        var testInvoker = new TestInvoker();
        var explicitFilterService = new ExplicitFilterService();
        var parallelLimitProvider = new ParallelLimitProvider();
        var hookMessagePublisher = new HookMessagePublisher(extension, messageBus);
        var globalStaticTestHookOrchestrator = new GlobalStaticTestHookOrchestrator(hookMessagePublisher);
        var assemblyHookOrchestrator =
            new AssemblyHookOrchestrator(hookMessagePublisher, globalStaticTestHookOrchestrator);
        var classHookOrchestrator = new ClassHookOrchestrator(hookMessagePublisher, globalStaticTestHookOrchestrator);
        var singleTestExecutor = new SingleTestExecutor(extension, disposer, cancellationTokenSource, testInvoker,
            explicitFilterService, parallelLimitProvider, assemblyHookOrchestrator, classHookOrchestrator, Logger);
        
        TestsExecutor = new TestsExecutor(singleTestExecutor, Logger, CommandLineOptions);
        
        OnEndExecutor = new OnEndExecutor(CommandLineOptions, Logger);
    }
    
    public async ValueTask DisposeAsync()
    {
        await StandardOutConsoleInterceptor.DisposeAsync();
        await StandardErrorConsoleInterceptor.DisposeAsync();
    }
}