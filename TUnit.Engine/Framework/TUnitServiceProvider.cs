using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.OutputDevice;
using Microsoft.Testing.Platform.Services;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;
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
    public TestDiscoveryService DiscoveryService { get; }
    public TestFactory TestFactory { get; }
    public UnifiedTestExecutor TestExecutor { get; }
    public TUnitMessageBus MessageBus { get; }
    public EngineCancellationToken CancellationToken { get; }
    
    [RequiresDynamicCode("Generic type resolution requires runtime type generation.")]
    [RequiresUnreferencedCode("Generic type resolution may access types not preserved by trimming.")]
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
        
        MessageBus = Register(new TUnitMessageBus(
            extension, 
            CommandLineOptions, 
            frameworkServiceProvider, 
            context));
        
        CancellationToken = Register(new EngineCancellationToken());
        
        // Create test services using new architecture
        var testInvoker = Register<ITestInvoker>(new TestInvoker());
        var hookInvoker = Register<IHookInvoker>(new HookInvoker());
        var dataSourceResolver = Register<IDataSourceResolver>(new DataSourceResolver());
        var genericTypeResolver = Register<IGenericTypeResolver>(new GenericTypeResolver());
        
        TestFactory = Register(new TestFactory(testInvoker, hookInvoker, dataSourceResolver, genericTypeResolver));
        
        // Initialize the test registry singleton
        TestRegistry.Initialize(TestFactory);
        
        // Get test metadata sources from registry
        var sources = TestMetadataRegistry.GetSources();
        
        // Check if reflection discovery is enabled
        var enableDynamicDiscovery = IsReflectionScannerEnabled(CommandLineOptions);
        
        DiscoveryService = Register(new TestDiscoveryService(
            sources,
            TestFactory,
            enableDynamicDiscovery));
        
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
    
    private bool IsReflectionScannerEnabled(ICommandLineOptions commandLineOptions)
    {
        // Default to false since GetOptionValue may not be available
        return false;
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