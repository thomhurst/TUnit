using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Services;

/// <summary>
/// Unified test builder that constructs tests from TestMetadata,
/// used by both source generation and reflection modes.
/// </summary>
internal class UnifiedTestBuilder(
    ContextManager contextManager,
    IServiceProvider serviceProvider)
{
    /// <summary>
    /// Builds a discovered test directly from TestMetadata.
    /// </summary>
    public DiscoveredTest BuildTest(TestMetadata metadata)
    {
        // Build test details using existing TestMetadata behavior
        var testDetails = metadata.BuildTestDetails();
        
        // Get class hook context
        var classHookContext = contextManager.GetClassHookContext(metadata.TestClassType);
        
        // Create test context
        var testContext = new TestContext(
            serviceProvider,
            testDetails,
            metadata,
            classHookContext
        );
        
        // Handle discovery exceptions
        if (metadata.DiscoveryException is not null)
        {
            testContext.SetResult(metadata.DiscoveryException);
        }
        
        // Run discovery hooks
        RunTestDiscoveryHooks(testDetails, testContext);
        
        // Build discovered test using existing TestMetadata behavior
        var discoveredTest = metadata.BuildDiscoveredTest(testContext);

        testContext.InternalDiscoveredTest = discoveredTest;
        
        return discoveredTest;
    }
    
    /// <summary>
    /// Builds multiple tests from dynamic test data.
    /// </summary>
    public IEnumerable<DiscoveredTest> BuildTests(DynamicTest dynamicTest)
    {
        return dynamicTest
            .BuildTestMetadatas()
            .Select(BuildTest);
    }
    
    private static void RunTestDiscoveryHooks(TestDetails testDetails, TestContext testContext)
    {
        var attributes = testDetails.DataAttributes
            .Concat(testDetails.Attributes)
            .Distinct();
        
        DiscoveredTestContext? discoveredTestContext = null;
        
        // Reverse to run assembly, then class, then method
        foreach (var attribute in attributes.OfType<ITestDiscoveryEventReceiver>().Reverse())
        {
            discoveredTestContext ??= new DiscoveredTestContext(testContext);
            attribute.OnTestDiscovery(discoveredTestContext);
        }
    }
}