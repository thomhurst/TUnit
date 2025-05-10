using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.OutputDevice;
using Microsoft.Testing.Platform.Services;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;
using TUnit.Engine.Capabilities;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;
#pragma warning disable TPEXP

namespace TUnit.Engine.Framework;

internal class TUnitServiceProvider : IServiceProvider, IAsyncDisposable
{
    private readonly ITestFrameworkCapabilities _capabilities;
    private readonly Dictionary<Type, object> _services = [];

    public ILoggerFactory LoggerFactory;
    public IOutputDevice OutputDevice;
    public ICommandLineOptions CommandLineOptions;

    public TUnitFrameworkLogger Logger { get; }
    public TUnitMessageBus TUnitMessageBus { get; }

    public HooksCollectorBase HooksCollector { get; set; }
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
    public EngineCancellationToken EngineCancellationToken { get; }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public TUnitServiceProvider(IExtension extension,
        ExecuteRequestContext context,
        IMessageBus messageBus,
        IServiceProvider frameworkServiceProvider, 
        ITestFrameworkCapabilities capabilities)
    {
        _capabilities = capabilities;
        
        Register(context);
        
        EngineCancellationToken = Register(new EngineCancellationToken());
        
        LoggerFactory = frameworkServiceProvider.GetLoggerFactory();
        
        OutputDevice = frameworkServiceProvider.GetOutputDevice();
        
        CommandLineOptions = frameworkServiceProvider.GetCommandLineOptions();

        Logger = Register(new TUnitFrameworkLogger(extension, OutputDevice, LoggerFactory.CreateLogger<TUnitFrameworkLogger>()));
        
        Initializer = Register(new TUnitInitializer(CommandLineOptions));
        
        StandardOutConsoleInterceptor = Register(new StandardOutConsoleInterceptor(CommandLineOptions));
        
        StandardErrorConsoleInterceptor = Register(new StandardErrorConsoleInterceptor(CommandLineOptions));

        FilterParser = Register(new FilterParser());

        var stringFilter = FilterParser.GetTestFilter(context);

        TUnitMessageBus = Register(new TUnitMessageBus(extension, CommandLineOptions, context));

        var instanceTracker = Register(new InstanceTracker());
        
        var isReflectionScannerEnabled = IsReflectionScannerEnabled(CommandLineOptions, Logger);

        HooksCollector = Register<HooksCollectorBase>
        (
            isReflectionScannerEnabled
                ? new ReflectionHooksCollector(context.Request.Session.SessionUid.Value)
                : new SourceGeneratedHooksCollector(context.Request.Session.SessionUid.Value)
        );

        var dependencyCollector = new DependencyCollector();
        
        var testMetadataCollector = Register(new TestsCollector(context.Request.Session.SessionUid.Value));

        var testsConstructor = Register<BaseTestsConstructor>
        (
            isReflectionScannerEnabled
#pragma warning disable IL3050
                ? new ReflectionTestsConstructor(extension, dependencyCollector, this)
#pragma warning restore IL3050
                : new SourceGeneratedTestsConstructor(extension, testMetadataCollector, dependencyCollector, this)
        );
        
        var testFilterService = Register(new TestFilterService(LoggerFactory));
        
        TestGrouper = Register(new TestGrouper());
        
        AssemblyHookOrchestrator = Register(new AssemblyHookOrchestrator(instanceTracker, HooksCollector, Logger));

        TestDiscoveryHookOrchestrator = Register(new TestDiscoveryHookOrchestrator(HooksCollector, Logger, stringFilter));
        TestSessionHookOrchestrator = Register(new TestSessionHookOrchestrator(HooksCollector, AssemblyHookOrchestrator, Logger, stringFilter));
        
        var classHookOrchestrator = Register(new ClassHookOrchestrator(instanceTracker, HooksCollector, Logger));
        
        var testHookOrchestrator = Register(new TestHookOrchestrator(HooksCollector, Logger));

        var testRegistrar = Register(new TestRegistrar(instanceTracker, AssemblyHookOrchestrator, classHookOrchestrator));
        
        Disposer = Register(new Disposer(Logger));
        
        var testInvoker = Register(new TestInvoker(testHookOrchestrator, Logger, Disposer));
        var parallelLimitProvider = Register(new ParallelLimitLockProvider());
        
        var singleTestExecutor = Register(new SingleTestExecutor(extension, instanceTracker, testInvoker, parallelLimitProvider, AssemblyHookOrchestrator, classHookOrchestrator, TUnitMessageBus, Logger, EngineCancellationToken, testRegistrar, GetCapability<StopExecutionCapability>()));
        
        TestsExecutor = Register(new TestsExecutor(singleTestExecutor, Logger, CommandLineOptions, EngineCancellationToken, AssemblyHookOrchestrator, classHookOrchestrator));
        
        TestDiscoverer = Register(new TUnitTestDiscoverer(testsConstructor, testFilterService, TestGrouper, testRegistrar, TUnitMessageBus, Logger, TestsExecutor));

        DynamicTestRegistrar = Register<IDynamicTestRegistrar>(new DynamicTestRegistrar(testsConstructor, testRegistrar,
            TestGrouper, TUnitMessageBus, TestsExecutor, EngineCancellationToken));
        
        TestFinder = Register(new TestsFinder(TestDiscoverer));
        Register<ITestFinder>(TestFinder);
        
        // TODO
        Register(new HookMessagePublisher(extension, messageBus));
        
        OnEndExecutor = Register(new OnEndExecutor(CommandLineOptions, Logger));
    }

    public IDynamicTestRegistrar DynamicTestRegistrar { get; }

    public Disposer Disposer { get; }

    public async ValueTask DisposeAsync()
    {
#if NET
        await StandardOutConsoleInterceptor.DisposeAsync();
        await StandardErrorConsoleInterceptor.DisposeAsync();
#else
        StandardOutConsoleInterceptor.Dispose();
        StandardErrorConsoleInterceptor.Dispose();
#endif

        foreach (var servicesValue in _services.Values)
        {
            await Disposer.DisposeAsync(servicesValue);
        }
    }

    private T Register<T>(T t)
    {
        _services.Add(typeof(T), t!);
        
        return t;
    }

    public object? GetService(Type serviceType)
    {
        _services.TryGetValue(serviceType, out object? result);
        return result;
    }
    
    public TCapability GetCapability<TCapability>()
        where TCapability : class, ITestFrameworkCapability
    {
        var capability = _capabilities.GetCapability<TCapability>();

        if (capability == null)
        {
            throw new InvalidOperationException($"No capability registered for {typeof(TCapability).Name}");
        }
        
        return capability;
    }
    
    private static bool IsReflectionScannerEnabled(ICommandLineOptions commandLineOptions, TUnitFrameworkLogger logger)
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            return false;
        }
#endif
        
        return IsReflectionScannerEnabledByCommandLine(commandLineOptions)
            || IsReflectionScannerEnabledByMsBuild();
    }

    private static bool IsReflectionScannerEnabledByCommandLine(ICommandLineOptions commandLineOptions)
    {
        return commandLineOptions.IsOptionSet(ReflectionScannerCommandProvider.ReflectionScanner);
    }
    
    private static bool IsReflectionScannerEnabledByMsBuild()
    {
        return Assembly.GetEntryAssembly()?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Any(x => x is { Key: "TUnitReflectionScanner", Value: "true" })
            ?? false;
    }
}