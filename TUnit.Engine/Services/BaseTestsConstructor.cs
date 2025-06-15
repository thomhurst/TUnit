using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Services;

internal abstract class BaseTestsConstructor(IExtension extension,
    DependencyCollector dependencyCollector,
    ContextManager contextManager,
    IServiceProvider serviceProvider) : IDataProducer
{
    public async Task<DiscoveredTest[]> GetTestsAsync(CancellationToken cancellationToken)
    {
        var discoveredTests = await DiscoverTestsAsync();

        dependencyCollector.ResolveDependencies(discoveredTests, cancellationToken);

        return discoveredTests;
    }

    protected abstract Task<DiscoveredTest[]> DiscoverTestsAsync();

    protected internal DiscoveredTest ConstructTest(TestMetadata testMetadata)
    {
        var testDetails = testMetadata.BuildTestDetails();

        var testContext = new TestContext(serviceProvider, testDetails, testMetadata, contextManager.GetClassHookContext(testDetails.TestClass.Type));

        if (testMetadata.DiscoveryException is not null)
        {
            testContext.SetResult(testMetadata.DiscoveryException);
        }

        RunOnTestDiscoveryAttributeHooks([..testDetails.DataAttributes, ..testDetails.Attributes], testContext);

        var discoveredTest = testMetadata.BuildDiscoveredTest(testContext);

        testContext.InternalDiscoveredTest = discoveredTest;

        return discoveredTest;
    }

    protected internal IEnumerable<DiscoveredTest> ConstructTests(DynamicTest dynamicTest)
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
