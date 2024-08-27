using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Extensions;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;
using TUnit.Engine.Models;
using TUnit.Engine.Services;

namespace TUnit.Engine.Framework;

internal sealed class TUnitTestFramework : ITestFramework, IDataProducer
{
    private readonly IExtension _extension;
    private readonly ITestFrameworkCapabilities _capabilities;
    private readonly ServiceProvider _serviceProvider;
    private readonly TUnitLogger _logger;
    private readonly TUnitTestDiscoverer _testDiscover;
    private readonly TestsExecutor _testsExecutor;
    private readonly TUnitInitializer _initializer;

    public TUnitTestFramework(IExtension extension,
        IServiceProvider frameworkServiceProvider,
        ITestFrameworkCapabilities capabilities)
    {
        _extension = extension;
        _capabilities = capabilities;
        
        _serviceProvider = new ServiceCollection()
            .AddTestEngineServices()
            .AddFromFrameworkServiceProvider(frameworkServiceProvider, extension)
            .BuildServiceProvider();

        _logger = _serviceProvider.GetRequiredService<TUnitLogger>();
        _testDiscover = _serviceProvider.GetRequiredService<TUnitTestDiscoverer>();
        _testsExecutor = _serviceProvider.GetRequiredService<TestsExecutor>();
        _initializer = _serviceProvider.GetRequiredService<TUnitInitializer>();
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
        
        await using (_serviceProvider)
        {
            try
            {
                _initializer.Initialize();

                await GlobalStaticTestHookOrchestrator.ExecuteBeforeHooks(new BeforeTestDiscoveryContext());
                
                var discoveredTests = _testDiscover.DiscoverTests(context.Request as TestExecutionRequest, context.CancellationToken);

                var failedToInitializeTests = TestDictionary.GetFailedToInitializeTests();
                
                await GlobalStaticTestHookOrchestrator.ExecuteAfterHooks(new TestDiscoveryContext(AssemblyHookOrchestrator.GetAllAssemblyHookContexts()));

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

                        var testSessionContext = new TestSessionContext(AssemblyHookOrchestrator.GetAllAssemblyHookContexts());
                        
                        await GlobalStaticTestHookOrchestrator.ExecuteBeforeHooks(testSessionContext);
                    
                        await _testsExecutor.ExecuteAsync(discoveredTests.AsParallel(), runTestExecutionRequest.Filter, context);
                        
                        await GlobalStaticTestHookOrchestrator.ExecuteAfterHooks(testSessionContext);

                        foreach (var artifact in testSessionContext.Artifacts)
                        {
                            await context.MessageBus.PublishAsync(this, new SessionFileArtifact(context.Request.Session.SessionUid, artifact.File, artifact.DisplayName, artifact.Description));
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(context.Request), context.Request.GetType().Name);
                }
            }
            catch (Exception e) when (e is TaskCanceledException or OperationCanceledException && context.CancellationToken.IsCancellationRequested)
            {
                await _logger.LogErrorAsync("The test run was cancelled.");
            }
            finally
            {
                await _serviceProvider
                    .GetRequiredService<TUnitOnEndExecutor>()
                    .ExecuteAsync();

                context.Complete();
            }
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