using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Services;

internal class TestsConstructor(IExtension extension, 
    TestsCollector testsCollector,
    DependencyCollector dependencyCollector, 
    IServiceProvider serviceProvider) : IDataProducer
{
    public DiscoveredTest[] GetTests(CancellationToken cancellationToken)
    {
        var testMetadatas = testsCollector.GetTests();
        
        var dynamicTests = testsCollector.GetDynamicTests();

        var discoveredTests = testMetadatas.
            Select(ConstructTest)
            .Concat(dynamicTests.SelectMany(ConstructTests))
            .ToArray();

        dependencyCollector.ResolveDependencies(discoveredTests, cancellationToken);
        
        return discoveredTests;
    }

    public DiscoveredTest ConstructTest(TestMetadata testMetadata)
    {
        var testDetails = testMetadata.BuildTestDetails();

        var testContext = new TestContext(serviceProvider, testDetails, testMetadata);

        if (testMetadata.DiscoveryException is not null)
        {
            testContext.SetResult(testMetadata.DiscoveryException);
        }

        RunOnTestDiscoveryAttributeHooks([..testDetails.DataAttributes, ..testDetails.Attributes], testContext);

        var discoveredTest = testMetadata.BuildDiscoveredTest(testContext);

        testContext.InternalDiscoveredTest = discoveredTest;

        return discoveredTest;
    }

    public IEnumerable<DiscoveredTest> ConstructTests(DynamicTest dynamicTest)
    {
        return dynamicTest.BuildTestMetadatas().Select(ConstructTest);
    }

    private static void RunOnTestDiscoveryAttributeHooks(IEnumerable<Attribute> attributes, TestContext testContext)
    {
        DiscoveredTestContext? discoveredTestContext = null;
        foreach (var onTestDiscoveryAttribute in attributes.OfType<ITestDiscoveryEventReceiver>().Reverse()) // Reverse to run assembly, then class, then method
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