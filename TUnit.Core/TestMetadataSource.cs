using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core;

/// <summary>
/// Test source that provides TestMetadata instances to be expanded by TestBuilder.
/// </summary>
public class TestMetadataSource : ITestSource
{
    private readonly IReadOnlyList<TestMetadata> _testMetadata;
    private readonly TestBuilder _testBuilder;
    
    public TestMetadataSource(IReadOnlyList<TestMetadata> testMetadata)
    {
        _testMetadata = testMetadata;
        _testBuilder = new TestBuilder();
    }
    
    public async Task<DiscoveryResult> DiscoverTestsAsync(string sessionId)
    {
        var allDefinitions = new List<ITestDefinition>();
        var failures = new List<DiscoveryFailure>();
        
        foreach (var metadata in _testMetadata)
        {
            try
            {
                // Skip if test is marked as skipped
                if (metadata.IsSkipped)
                {
                    // Still need to create a test definition for skipped tests
                    // so they show up in test runners as skipped
                    var skippedDefinition = CreateSkippedTestDefinition(metadata);
                    allDefinitions.Add(skippedDefinition);
                    continue;
                }
                
                // Build all test definitions from metadata
                var definitions = await _testBuilder.BuildTestsAsync(metadata);
                allDefinitions.AddRange(definitions);
            }
            catch (Exception ex)
            {
                // Record discovery failure
                failures.Add(new DiscoveryFailure
                {
                    TestId = metadata.TestIdTemplate,
                    TestMethodName = metadata.MethodMetadata.Name,
                    TestFilePath = metadata.TestFilePath,
                    TestLineNumber = metadata.TestLineNumber,
                    Exception = ex
                });
            }
        }
        
        return new DiscoveryResult
        {
            TestDefinitions = allDefinitions,
            DiscoveryFailures = failures
        };
    }
    
    private TestDefinition CreateSkippedTestDefinition(TestMetadata metadata)
    {
        // Create a simple test definition for skipped tests
        return new TestDefinition
        {
            TestId = metadata.TestIdTemplate.Replace("{TestIndex}", "0").Replace("{RepeatIndex}", "0"),
            MethodMetadata = metadata.MethodMetadata,
            TestFilePath = metadata.TestFilePath,
            TestLineNumber = metadata.TestLineNumber,
            TestClassFactory = () => throw new InvalidOperationException("Skipped test should not be instantiated"),
            TestMethodInvoker = (_, _) => throw new InvalidOperationException("Skipped test should not be invoked"),
            ClassArgumentsProvider = () => Array.Empty<object?>(),
            MethodArgumentsProvider = () => Array.Empty<object?>(),
            PropertiesProvider = () => new Dictionary<string, object?>()
        };
    }
}