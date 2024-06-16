using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Extensions;
using ServiceProviderServiceExtensions = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions;

namespace TUnit.Engine;

internal sealed class TUnitTestFramework : ITestFramework, IDataProducer
{
    private readonly IExtension _extension;
    private readonly ITestFrameworkCapabilities _capabilities;
    private readonly ServiceProvider _myServiceProvider;
    private readonly TUnitLogger _logger;

    public TUnitTestFramework(IExtension extension,
        IServiceProvider serviceProvider,
        ITestFrameworkCapabilities capabilities)
    {
        _extension = extension;
        _capabilities = capabilities;
        
        _myServiceProvider = new ServiceCollection()
            .AddTestEngineServices()
            .AddFromFrameworkServiceProvider(serviceProvider, extension)
            .BuildServiceProvider();

        _logger = _myServiceProvider.GetRequiredService<TUnitLogger>();
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
        EngineCancellationToken.Initialise(context.CancellationToken);
        
        var stopwatch = new Stopwatch();
        
        await using (_myServiceProvider)
        {
            try
            {
                var discoveredTests = _myServiceProvider
                    .GetRequiredService<TUnitTestDiscoverer>()
                    .DiscoverTests(context.Request as TestExecutionRequest, context.CancellationToken)
                    .ToList();

                var failedToInitializeTests = TestDictionary.GetFailedToInitializeTests();

                if (context.Request is DiscoverTestExecutionRequest)
                {
                    foreach (var testInformation in discoveredTests)
                    {
                        var testNode = testInformation.TestNode;
                        testNode.Properties.Add(DiscoveredTestNodeStateProperty.CachedInstance);

                        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                            sessionUid: context.Request.Session.SessionUid,
                            testNode: testNode)
                        );
                    }

                    await NotifyFailedTests(context, failedToInitializeTests, true);
                }
                else if (context.Request is RunTestExecutionRequest runTestExecutionRequest)
                {
                    stopwatch.Start();

                    await NotifyFailedTests(context, failedToInitializeTests, false);

                    await _myServiceProvider.GetRequiredService<TestsExecutor>()
                        .ExecuteAsync(discoveredTests, runTestExecutionRequest.Filter, context);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(context.Request), context.Request.GetType().Name);
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("Tests aren't safe! - We shouldn't be throwing exceptions here", e);
            }
            finally
            {
                var time = stopwatch.Elapsed;

                await _logger.LogInformationAsync($"Time elapsed: {time}");

                await ServiceProviderServiceExtensions
                    .GetRequiredService<TUnitOnEndExecutor>(_myServiceProvider)
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
                    isDiscovered ? DiscoveredTestNodeStateProperty.CachedInstance : new FailedTestNodeStateProperty(failedToInitializeTest.Exception, "Test failed to initialize"),
                    new TestFileLocationProperty(failedToInitializeTest.TestFilePath,
                        new LinePositionSpan(new LinePosition(failedToInitializeTest.TestLineNumber, 0),
                            new LinePosition(failedToInitializeTest.TestLineNumber, 0))
                    ),
                    
                    // TRX Reports
                    new KeyValuePairStringProperty("trxreport.exceptionmessage", failedToInitializeTest.Exception.Message),
                    new KeyValuePairStringProperty("trxreport.exceptionstacktrace", failedToInitializeTest.Exception.StackTrace!)
                )
            };
                        
            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                sessionUid: context.Request.Session.SessionUid,
                testNode: failedNode)
            );
        }
    }

    public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        return Task.FromResult(new CloseTestSessionResult
        {
            IsSuccess = true
        });
    }

    public Type[] DataTypesProduced { get; } =
    [
        typeof(TestNodeUpdateMessage)
    ];
}