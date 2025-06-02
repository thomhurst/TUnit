using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.Services;
using Polyfills;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Capabilities;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Helpers;
using TUnit.Engine.Logging;
#pragma warning disable TPEXP

namespace TUnit.Engine.Framework;

internal sealed class TUnitTestFramework : ITestFramework, IDataProducer
{
    private readonly IExtension _extension;
    private readonly IServiceProvider _frameworkServiceProvider;
    private readonly ITestFrameworkCapabilities _capabilities;
    private readonly IEnumerable<IFilterReceiver> _filterReceivers;
    private static readonly ConcurrentDictionary<string, TUnitServiceProvider> ServiceProvidersPerSession = [];

    public TUnitTestFramework(IExtension extension,
        IServiceProvider frameworkServiceProvider,
        ITestFrameworkCapabilities capabilities,
        IEnumerable<IFilterReceiver> filterReceivers)
    {
        _extension = extension;
        _frameworkServiceProvider = frameworkServiceProvider;
        _capabilities = capabilities;
        _filterReceivers = filterReceivers;

        TestContext.Configuration = new ConfigurationAdapter(frameworkServiceProvider.GetConfiguration());
    }

    public Task<bool> IsEnabledAsync() => _extension.IsEnabledAsync();

    public string Uid => _extension.Uid;
    public string Version => _extension.Version;
    public string DisplayName => _extension.DisplayName;
    public string Description => _extension.Description;
    
    public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        while(Sources.AssemblyLoaders.TryDequeue(out var assemblyLoader))
        {
            TryLoadAssembly(assemblyLoader);
        }
        
        return Task.FromResult(new CreateTestSessionResult
        {
            IsSuccess = true
        });
    }

    public async Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
#if LAUNCH_DEBUGGER
        System.Diagnostics.Debugger.Launch();
#endif

        var serviceProvider = ServiceProvidersPerSession.GetOrAdd(context.Request.Session.SessionUid.Value,
            _ => new TUnitServiceProvider(_extension, context, context.MessageBus, _frameworkServiceProvider, _capabilities)
        );
        
        _capabilities.GetCapability<StopExecutionCapability>()!
            .OnStopRequested += async (_, _) =>
        {
            await serviceProvider.EngineCancellationToken.CancellationTokenSource.CancelAsync();
        };

        var stringFilter = serviceProvider.FilterParser.GetTestFilter(context);
        
        foreach (var filterReceiver in _filterReceivers)
        {
            filterReceiver.Filter = stringFilter;
        }

        var logger = serviceProvider.Logger;
        
        GlobalContext.Current = new GlobalContext
        {
            TestFilter = stringFilter,
            GlobalLogger = logger,
            OriginalConsoleOut = StandardOutConsoleInterceptor.DefaultOut,
            OriginalConsoleError = StandardErrorConsoleInterceptor.DefaultError
        };
        
        serviceProvider.StandardOutConsoleInterceptor.Initialize();
        serviceProvider.StandardErrorConsoleInterceptor.Initialize();
        
        serviceProvider.Initializer.Initialize();
        
        var beforeTestDiscoveryContext = serviceProvider.ContextManager.BeforeTestDiscoveryContext;

        var testSessionContext = serviceProvider.ContextManager.TestSessionContext;
        
        try
        {
            serviceProvider.EngineCancellationToken.Initialise(context.CancellationToken);

            await serviceProvider.TestDiscoveryHookOrchestrator.RunBeforeTestDiscovery(beforeTestDiscoveryContext);
            
            beforeTestDiscoveryContext.RestoreExecutionContext();

            serviceProvider.ContextManager.AfterTestDiscoveryContext.AddTests(
                serviceProvider.TestDiscoverer.GetTests().Select(x => x.TestContext)
            );
            
            var afterDiscoveryHooks = serviceProvider.TestDiscoveryHookOrchestrator.CollectAfterHooks();
            
            foreach (var afterDiscoveryHook in afterDiscoveryHooks)
            {
                try
                {
                    await afterDiscoveryHook.ExecuteAsync(serviceProvider.ContextManager.AfterTestDiscoveryContext, CancellationToken.None);
                }
                catch (Exception e)
                {
                    throw new HookFailedException($"Error executing [After(TestDiscovery)] hook: {afterDiscoveryHook.MethodInfo.Type.FullName}.{afterDiscoveryHook.Name}", e);
                }
            }
            
            testSessionContext.RestoreExecutionContext();

            var filteredTests = await serviceProvider.TestDiscoverer
                .FilterTests(context, serviceProvider.EngineCancellationToken.Token);
            
            switch (context.Request)
            {
                case DiscoverTestExecutionRequest:
                {
                    foreach (var test in filteredTests.AllValidTests)
                    {
                        await serviceProvider.TUnitMessageBus.Discovered(test.TestContext);
                    }
                    break;
                }
                case RunTestExecutionRequest runTestExecutionRequest:
                    
                    TestSessionContext.Current = testSessionContext;

                    await serviceProvider.TestSessionHookOrchestrator.RunBeforeTestSession(testSessionContext, context.CancellationToken);
                    
                    await serviceProvider.TestsExecutor.ExecuteAsync(filteredTests, runTestExecutionRequest.Filter,
                        context.CancellationToken);

                    // Tests could reschedule separate invocations - This allows us to wait for all invocations
                    await serviceProvider.TestsExecutor.WaitForFinishAsync();

                    var afterSessionHooks = serviceProvider.TestSessionHookOrchestrator.CollectAfterHooks();

                    foreach (var afterSessionHook in afterSessionHooks)
                    {
                        try
                        {
                            await afterSessionHook.ExecuteAsync(testSessionContext, context.CancellationToken);
                        }
                        catch (Exception e)
                        {
                            throw new HookFailedException($"Error executing [After(TestSession)] hook: {afterSessionHook.MethodInfo.Type.FullName}.{afterSessionHook.Name}", e);
                        }
                    }
                    
                    foreach (var artifact in testSessionContext.Artifacts)
                    {
                        await serviceProvider.TUnitMessageBus.SessionArtifact(artifact);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(context.Request), context.Request.GetType().Name);
            }
        }
        catch (Exception e) when (e is TaskCanceledException or OperationCanceledException &&
                                  context.CancellationToken.IsCancellationRequested)
        {
            await logger.LogErrorAsync("The test run was cancelled.");
        }
        catch (Exception e)
        {
            await logger.LogErrorAsync(e);
            
            await context.MessageBus.PublishAsync(
                dataProducer: this,
                data: new TestNodeUpdateMessage(
                    sessionUid: context.Request.Session.SessionUid,
                    testNode: new TestNode
                    {
                        DisplayName = $"Unhandled exception - {e.GetType().Name}: {e.Message}",
                        Uid = Guid.NewGuid().ToString(),
                        Properties = new PropertyBag(new ErrorTestNodeStateProperty(e))
                    }));
        }
        finally
        {
            await serviceProvider.OnEndExecutor.ExecuteAsync(testSessionContext);

            context.Complete();
        }
    }

    public async Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        try
        {
            await using var _ = ServiceProvidersPerSession[context.SessionUid.Value];
            
            return new CloseTestSessionResult
            {
                IsSuccess = true
            };
        }
        catch (Exception e)
        {
            return new CloseTestSessionResult
            {
                IsSuccess = false,
                ErrorMessage = e.Message
            };
        }
    }

    public Type[] DataTypesProduced { get; } =
    [
        typeof(TestNodeUpdateMessage)
    ];

    private static void TryLoadAssembly(Func<Assembly> assemblyLoader)
    {
        try
        {
            assemblyLoader.Invoke();
        }
        catch
        {
            // ignored
        }
    }
}