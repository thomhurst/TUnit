using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Services;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;
using TUnit.Engine.Building;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine.Framework;

/// <summary>
/// Service provider for the TUnit framework
/// </summary>
internal class TUnitServiceProvider : IServiceProvider, IAsyncDisposable
{
    private readonly Dictionary<Type, object> _services = new();

    // Core services
    public TUnitFrameworkLogger Logger { get; }
    public ICommandLineOptions CommandLineOptions { get; }
    public TestDiscoveryServiceV2 DiscoveryService { get; }
    public UnifiedTestBuilderPipeline TestBuilderPipeline { get; }
    public UnifiedTestExecutor TestExecutor { get; }
    public TUnitMessageBus MessageBus { get; }
    public EngineCancellationToken CancellationToken { get; }
    public TestFilterService TestFilterService { get; }

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
        Logger = Register(new TUnitFrameworkLogger(
            extension,
            outputDevice,
            loggerFactory.CreateLogger<TUnitFrameworkLogger>(),
            CommandLineOptions));

        TestFilterService = Register(new TestFilterService(loggerFactory));

        MessageBus = Register(new TUnitMessageBus(
            extension,
            CommandLineOptions,
            frameworkServiceProvider,
            context));

        CancellationToken = Register(new EngineCancellationToken());

        // Create test services using unified architecture
        var testInvoker = Register<ITestInvoker>(new TestInvoker());
        var hookInvoker = Register<IHookInvoker>(new HookInvoker());

        // Detect execution mode from command line or environment
        var executionMode = GetExecutionMode(CommandLineOptions);
        
        // Create pipeline based on execution mode
        TestBuilderPipeline = Register(
            UnifiedTestBuilderPipelineFactory.CreatePipeline(
                executionMode, this, assembliesToScan: null));

        DiscoveryService = Register(new TestDiscoveryServiceV2(TestBuilderPipeline, 
            enableDynamicDiscovery: executionMode == Core.Enums.TestExecutionMode.Reflection));

        // Create single test executor with ExecutionContext support
        var singleTestExecutor = Register<ISingleTestExecutor>(
            new SingleTestExecutor(Logger));

        TestExecutor = Register(new UnifiedTestExecutor(
            singleTestExecutor,
            CommandLineOptions,
            Logger,
            loggerFactory));

        // Set session IDs for proper test reporting
        var sessionUid = context.Request.Session.SessionUid;
        singleTestExecutor.SetSessionId(sessionUid);
        TestExecutor.SetSessionId(sessionUid);
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

    private Core.Enums.TestExecutionMode GetExecutionMode(ICommandLineOptions commandLineOptions)
    {
        // Check for command line option first
        if (commandLineOptions.TryGetOptionArgumentList("tunit-execution-mode", out var modes) && modes.Length > 0)
        {
            if (Enum.TryParse<Core.Enums.TestExecutionMode>(modes[0], ignoreCase: true, out var mode))
            {
                return mode;
            }
        }

        // Check environment variable
        var envMode = Environment.GetEnvironmentVariable("TUNIT_EXECUTION_MODE");
        if (!string.IsNullOrEmpty(envMode) && 
            Enum.TryParse<Core.Enums.TestExecutionMode>(envMode, ignoreCase: true, out var envModeEnum))
        {
            return envModeEnum;
        }

        // Default to auto-detect based on available tests
        return Core.Enums.TestExecutionMode.SourceGeneration;
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
