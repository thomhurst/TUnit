using System.Collections.Concurrent;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Hooks;

namespace TUnit.Engine.Framework;

internal sealed class TUnitTestFramework : ITestFramework, IDataProducer
{
    private readonly IExtension _extension;
    private readonly IServiceProvider _frameworkServiceProvider;
    private readonly ITestFrameworkCapabilities _capabilities;
    private static readonly ConcurrentDictionary<string, TUnitServiceProvider> ServiceProviders = [];

    public TUnitTestFramework(IExtension extension,
        IServiceProvider frameworkServiceProvider,
        ITestFrameworkCapabilities capabilities)
    {
        _extension = extension;
        _frameworkServiceProvider = frameworkServiceProvider;
        _capabilities = capabilities;
    }

    public Task<bool> IsEnabledAsync() => _extension.IsEnabledAsync();

    public string Uid => _extension.Uid;
    public string Version => _extension.Version;
    public string DisplayName => _extension.DisplayName;
    public string Description => _extension.Description;
    
    public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
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

        var serviceProvider = ServiceProviders.GetOrAdd(context.Request.Session.SessionUid.Value,
            _ => new TUnitServiceProvider(_extension, context, context.MessageBus, _frameworkServiceProvider)
        );
        
        GlobalContext.Current.GlobalLogger = serviceProvider.Logger;
        
        serviceProvider.StandardOutConsoleInterceptor.Initialize();
        serviceProvider.StandardErrorConsoleInterceptor.Initialize();
        
        serviceProvider.Initializer.Initialize();

        var stringFilter = serviceProvider.FilterParser.GetTestFilter(context);

        TestSessionContext? testSessionContext = null;
        try
        {
            EngineCancellationToken.Initialise(context.CancellationToken);
            
            var filteredTests = await serviceProvider.TestDiscoverer
                .FilterTests(context, stringFilter, EngineCancellationToken.Token);

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
                    testSessionContext =
                        new TestSessionContext(AssemblyHookOrchestrator.GetAllAssemblyHookContexts())
                        {
                            TestFilter = stringFilter
                        };

                    await GlobalStaticTestHookOrchestrator.ExecuteBeforeHooks(testSessionContext);

                    await serviceProvider.TestsExecutor.ExecuteAsync(filteredTests, runTestExecutionRequest.Filter,
                        context);

                    await GlobalStaticTestHookOrchestrator.ExecuteAfterHooks(testSessionContext);

                    foreach (var artifact in testSessionContext.Artifacts)
                    {
                        await serviceProvider.TUnitMessageBus.Artifact(artifact);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(context.Request), context.Request.GetType().Name);
            }
        }
        catch (Exception e) when (e is TaskCanceledException or OperationCanceledException &&
                                  context.CancellationToken.IsCancellationRequested)
        {
            await serviceProvider.Logger.LogErrorAsync("The test run was cancelled.");
        }
        catch (Exception e)
        {
            await serviceProvider.Logger.LogErrorAsync(e);
            
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
            await using var _ = ServiceProviders[context.SessionUid.Value];
            
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
        typeof(TestNodeUpdateMessage),
        typeof(SessionFileArtifact)
    ];
}