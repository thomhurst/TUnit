using System;
using System.Collections.Generic;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.OutputDevice;
using Microsoft.Testing.Platform.Services;
using TUnit.Core;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine.Framework;

/// <summary>
/// Simplified service provider that uses the new architecture
/// </summary>
internal class SimplifiedTUnitServiceProvider : IServiceProvider, IAsyncDisposable
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
    
    public SimplifiedTUnitServiceProvider(
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
        var testInvoker = Register<ITestInvoker>(new DefaultTestInvoker());
        var hookInvoker = Register<IHookInvoker>(new DefaultHookInvoker());
        var dataSourceResolver = Register<IDataSourceResolver>(new DefaultDataSourceResolver());
        
        TestFactory = Register(new TestFactory(testInvoker, hookInvoker, dataSourceResolver));
        
        // Get test metadata sources from registry
        var sources = TestMetadataRegistry.GetSources();
        
        // Check if reflection discovery is enabled
        var enableDynamicDiscovery = IsReflectionScannerEnabled(CommandLineOptions);
        
        DiscoveryService = Register(new TestDiscoveryService(
            sources,
            TestFactory,
            enableDynamicDiscovery));
        
        // Create single test executor
        var singleTestExecutor = Register<ISingleTestExecutor>(
            new DefaultSingleTestExecutor(Logger));
        
        TestExecutor = Register(new UnifiedTestExecutor(
            singleTestExecutor,
            CommandLineOptions,
            Logger));
            
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
        // For now, default to false since GetOptionValue may not be available
        // TODO: Implement proper command line option reading
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