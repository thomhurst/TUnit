using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.Services;
using TUnit.Core;
using TUnit.Core.Logging;
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
        
        EngineCancellationToken.Initialise(context.CancellationToken);
        
        _serviceProvider.StandardOutConsoleInterceptor.Initialize();
        _serviceProvider.StandardErrorConsoleInterceptor.Initialize();

        try
        {
            _serviceProvider.Initializer.Initialize();

            await GlobalStaticTestHookOrchestrator.ExecuteBeforeHooks(new BeforeTestDiscoveryContext());

            var discoveredTests = _serviceProvider.TestDiscoverer.DiscoverTests(context.Request as TestExecutionRequest,
                context.CancellationToken);

            var failedToInitializeTests = _serviceProvider.TestDiscoverer.GetFailedToInitializeTests();

            var organisedTests = _serviceProvider.TestGrouper.OrganiseTests(discoveredTests);
            foreach (var test in organisedTests.AllTests)
            {
                TestRegistrar.RegisterInstance(test.TestContext);
            }

            await GlobalStaticTestHookOrchestrator.ExecuteAfterHooks(
                new TestDiscoveryContext(AssemblyHookOrchestrator.GetAllAssemblyHookContexts()));

            switch (context.Request)
            {
                case DiscoverTestExecutionRequest:
                {
                    foreach (var testNode in discoveredTests.Select(testInformation => testInformation.ToTestNode()))
                    {
                        testNode.Properties.Add(DiscoveredTestNodeStateProperty.CachedInstance);

                        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                            sessionUid: context.Request.Session.SessionUid,
                            testNode: testNode)
                        );
                    }

                    await NotifyFailedTests(context, failedToInitializeTests, true);
                    break;
                }
                case RunTestExecutionRequest runTestExecutionRequest:
                    await NotifyFailedTests(context, failedToInitializeTests, false);

                    var testSessionContext =
                        new TestSessionContext(AssemblyHookOrchestrator.GetAllAssemblyHookContexts());

                    await GlobalStaticTestHookOrchestrator.ExecuteBeforeHooks(testSessionContext);

                    await _serviceProvider.TestsExecutor.ExecuteAsync(organisedTests, runTestExecutionRequest.Filter,
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
        }
        finally
        {
            await _serviceProvider.OnEndExecutor.ExecuteAsync();

            context.Complete();
        }
    }

    private async Task NotifyFailedTests(ExecuteRequestContext context,
        IEnumerable<FailedInitializationTest> failedToInitializeTests, bool isDiscovered)
    {
        foreach (var failedToInitializeTest in failedToInitializeTests)
        {
            var failedNode = new TestNode
            {
                Uid = failedToInitializeTest.TestId,
                DisplayName = failedToInitializeTest.DisplayName,
                Properties = new PropertyBag(
                    isDiscovered ? DiscoveredTestNodeStateProperty.CachedInstance : new ErrorTestNodeStateProperty(failedToInitializeTest.Exception, "Test failed to initialize"),
                    new TestFileLocationProperty(failedToInitializeTest.TestFilePath,
                        new LinePositionSpan(new LinePosition(failedToInitializeTest.TestLineNumber, 0),
                            new LinePosition(failedToInitializeTest.TestLineNumber, 0))
                    ),
                    
                    // TRX Reports
                    new TrxExceptionProperty(failedToInitializeTest.Exception.Message, failedToInitializeTest.Exception.StackTrace)
                )
            };
                        
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                sessionUid: context.Request.Session.SessionUid,
                testNode: failedNode)
            );
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