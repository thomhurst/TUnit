using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Services;

[SuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
[SuppressMessage("Trimming", "IL2070:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The parameter of method does not have matching annotations.")]
internal class TestsConstructor(IExtension extension, 
    TestsCollector testsCollector,
    DependencyCollector dependencyCollector, 
    IServiceProvider serviceProvider) : IDataProducer
{
    public DiscoveredTest[] GetTests(CancellationToken cancellationToken)
    {
        var discoveredTests = IsReflectionScannerEnabled()
            ? GetByReflectionScanner()
            : GetBySourceGenerationRegistration();
        
        dependencyCollector.ResolveDependencies(discoveredTests, cancellationToken);
        
        return discoveredTests;
    }

    private static DiscoveredTest[] GetByReflectionScanner()
    {
        var testMethods = Assembly.GetEntryAssembly()
            !.GetTypes()
            .SelectMany(x => x.GetMethods())
            .Where(x => x.GetCustomAttributes<TestAttribute>().Any())
            .ToArray();

        return [];
    }

    private DiscoveredTest[] GetBySourceGenerationRegistration()
    {
        var testMetadatas = testsCollector.GetTests();
        
        var dynamicTests = testsCollector.GetDynamicTests();

        var discoveredTests = testMetadatas.
            Select(ConstructTest)
            .Concat(dynamicTests.SelectMany(ConstructTests))
            .ToArray();
        
        return discoveredTests;
    }

    private static bool IsReflectionScannerEnabled()
    {
        return Assembly.GetEntryAssembly()?
            .GetCustomAttributes()
            .OfType<AssemblyMetadataAttribute>()
            .FirstOrDefault(x => x.Key == "TUnit.ReflectionScanner")
            ?.Value == "true";
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