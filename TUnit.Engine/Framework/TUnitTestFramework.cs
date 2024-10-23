using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.Services;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Extensions;
using TUnit.Engine.Hooks;
using TUnit.Engine.Models;

namespace TUnit.Engine.Framework;

internal sealed class TUnitTestFramework : ITestFramework, IDataProducer
{
    private readonly IExtension _extension;
    private readonly ITestFrameworkCapabilities _capabilities;
    private readonly TUnitServiceProvider _serviceProvider;

    public TUnitTestFramework(IExtension extension,
        IServiceProvider frameworkServiceProvider,
        ITestFrameworkCapabilities capabilities)
    {
        _extension = extension;
        _capabilities = capabilities;
        
        _serviceProvider = new TUnitServiceProvider(extension, frameworkServiceProvider.GetMessageBus(), frameworkServiceProvider);
        
        GlobalContext.Current.GlobalLogger = _serviceProvider.Logger;
        
        _serviceProvider.StandardOutConsoleInterceptor.Initialize();
        _serviceProvider.StandardErrorConsoleInterceptor.Initialize();
        
        _serviceProvider.Initializer.Initialize();
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

        var stringFilter = _serviceProvider.FilterParser.GetTestFilter(context);

        TestSessionContext? testSessionContext = null;
        try
        {
            EngineCancellationToken.Initialise(context.CancellationToken);
            
            var filteredTests = await _serviceProvider.TestDiscoverer
                .FilterTests(context, stringFilter, EngineCancellationToken.Token);

            switch (context.Request)
            {
                case DiscoverTestExecutionRequest:
                {
                    foreach (var testNode in filteredTests.AllValidTests.Select(testInformation => testInformation.ToTestNode()))
                    {
                        testNode.Properties.Add(DiscoveredTestNodeStateProperty.CachedInstance);

                        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                            sessionUid: context.Request.Session.SessionUid,
                            testNode: testNode)
                        );
                    }

                    await _serviceProvider.FailedInitializationTestPublisher.NotifyFailedTests(context,
                        filteredTests.FailedInitialization, true);

                    break;
                }
                case RunTestExecutionRequest runTestExecutionRequest:
                    
                    await _serviceProvider.FailedInitializationTestPublisher.NotifyFailedTests(context,
                        filteredTests.FailedInitialization, false);

                    testSessionContext =
                        new TestSessionContext(AssemblyHookOrchestrator.GetAllAssemblyHookContexts())
                        {
                            TestFilter = stringFilter
                        };

                    await GlobalStaticTestHookOrchestrator.ExecuteBeforeHooks(testSessionContext);

                    await _serviceProvider.TestsExecutor.ExecuteAsync(filteredTests, runTestExecutionRequest.Filter,
                        context);

                    await GlobalStaticTestHookOrchestrator.ExecuteAfterHooks(testSessionContext);

                    foreach (var artifact in testSessionContext.Artifacts)
                    {
                        await context.MessageBus.PublishAsync(this,
                            new SessionFileArtifact(context.Request.Session.SessionUid, artifact.File,
                                artifact.DisplayName, artifact.Description));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(context.Request), context.Request.GetType().Name);
            }
        }
        catch (Exception e) when (e is TaskCanceledException or OperationCanceledException &&
                                  context.CancellationToken.IsCancellationRequested)
        {
            await _serviceProvider.Logger.LogErrorAsync("The test run was cancelled.");
        }
        catch (Exception e)
        {
            await _serviceProvider.Logger.LogErrorAsync(e);
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
            await _serviceProvider.OnEndExecutor.ExecuteAsync(testSessionContext);

            context.Complete();
        }
    }

    public async Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        try
        {
            await using var _ = _serviceProvider;
            
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