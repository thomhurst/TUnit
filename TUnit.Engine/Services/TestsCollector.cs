using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Logging;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services;

internal class TestsCollector(IExtension extension, ILoggerFactory loggerFactory) : IDataProducer
{
    private readonly ILogger<TestsCollector> _logger = loggerFactory.CreateLogger<TestsCollector>();

    public IEnumerable<DiscoveredTest> GetTests(ExecuteRequestContext context)
    {
        var count = 0;
        
        foreach (var sourceGeneratedTestNode in TestDictionary.TestSources
                     .AsParallel()
                     .SelectMany(x => x.CollectTests()))
        {
            count++;

            if (sourceGeneratedTestNode.FailedInitializationTest is {} failedInitializationTest)
            {
                context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                    context.Request.Session.SessionUid,
                    new TestNode
                    {
                        Uid = failedInitializationTest.TestId,
                        DisplayName = $"{failedInitializationTest.TestName} (Failed Initialization)",
                        Properties = new PropertyBag
                        (
                            new ErrorTestNodeStateProperty(failedInitializationTest.Exception, "Error initializing test")
                        )
                    }));
                continue;
            }
            
            yield return MapTest(sourceGeneratedTestNode.TestMetadata!);
        }
        
        _logger.LogTrace($"Found {count} before filtering.");
    }

    private DiscoveredTest MapTest(TestMetadata testMetadata)
    {
        var testDetails = testMetadata.BuildTestDetails();

        var testContext = new TestContext(testDetails, testMetadata.ObjectBag);

        RunOnTestDiscoveryAttributeHooks([..testDetails.DataAttributes, ..testDetails.Attributes], testContext);

        var discoveredTest = testMetadata.BuildDiscoveredTest(testContext);

        testContext.InternalDiscoveredTest = discoveredTest;

        return discoveredTest;
    }

    private static void RunOnTestDiscoveryAttributeHooks(IEnumerable<Attribute> attributes, TestContext testContext)
    {
        DiscoveredTestContext? discoveredTestContext = null;
        foreach (var onTestDiscoveryAttribute in attributes.OfType<ITestDiscoveryEvent>().Reverse()) // Reverse to run assembly, then class, then method
        {
            onTestDiscoveryAttribute.OnTestDiscovery(discoveredTestContext ??= new DiscoveredTestContext(testContext));
        }
    }
    
    
    public Task<bool> IsEnabledAsync()
    {
        return extension.IsEnabledAsync();
    }

    public string Uid => extension.Uid;
    public string Version => extension.Version;
    public string DisplayName => extension.DisplayName;
    public string Description => extension.Description;
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];
}