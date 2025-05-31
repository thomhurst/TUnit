#pragma warning disable TPEXP

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Services;

internal class TestFilterService(ILoggerFactory loggerFactory)
{
    private readonly ILogger<TestFilterService> _logger = loggerFactory.CreateLogger<TestFilterService>();

    public IReadOnlyCollection<DiscoveredTest> FilterTests(TestExecutionRequest? testExecutionRequest, IReadOnlyCollection<DiscoveredTest> testNodes)
    {
        var testExecutionFilter = testExecutionRequest?.Filter;
        
        if (testExecutionFilter is null or NopFilter)
        {
            _logger.LogTrace("No test filter found.");

            if (testExecutionRequest is RunTestExecutionRequest)
            {
                return testNodes
                    .Where(x => !x.TestDetails.Attributes.OfType<ExplicitAttribute>().Any())
                    .ToArray();
            }

            return testNodes;
        }
        
        _logger.LogTrace($"Test filter is: {testExecutionFilter.GetType().Name}");

        var filteredTests = testNodes.Where(x => MatchesTest(testExecutionFilter, x)).ToArray();
        
        var testsWithExplicitAttributeCount = filteredTests.Count(x => x.TestDetails.Attributes.OfType<ExplicitAttribute>().Any());
        
        if (testsWithExplicitAttributeCount > 0 && testsWithExplicitAttributeCount < filteredTests.Length)
        {
            return testNodes
                .Where(x => !x.TestDetails.Attributes.OfType<ExplicitAttribute>().Any())
                .ToArray();
        }

        return filteredTests;
    }

    public bool MatchesTest(ITestExecutionFilter? testExecutionFilter, DiscoveredTest discoveredTest)
    {
#pragma warning disable TPEXP
        var shouldRunTest = testExecutionFilter switch
        {
            null => true,
            NopFilter => true,
            TestNodeUidListFilter testNodeUidListFilter => testNodeUidListFilter.TestNodeUids.Contains(new TestNodeUid(discoveredTest.TestDetails.TestId)),
            TreeNodeFilter treeNodeFilter => treeNodeFilter.MatchesFilter(BuildPath(discoveredTest.TestDetails), BuildPropertyBag(discoveredTest.TestDetails)),
            _ => UnhandledFilter(testExecutionFilter)
        };

        return shouldRunTest;
#pragma warning restore TPEXP
    }

    private string BuildPath(TestDetails testDetails)
    {
        var assembly = testDetails.TestClass.Type.Assembly.GetName();

        var classTypeName = testDetails.TestClass.Type.Name;
        
        return
            $"/{assembly.Name ?? assembly.FullName}/{testDetails.TestClass.Type.Namespace}/{classTypeName}/{testDetails.TestMethod.Name}";
    }

    private PropertyBag BuildPropertyBag(TestDetails testDetails)
    {
        var properties = testDetails.ExtractProperties();

        var categories = testDetails.Categories.Select(x => new TestMetadataProperty(x));
        
        return new PropertyBag(
            [
                ..properties,
                ..categories,
                ..testDetails.Categories.Select(x => new KeyValuePairStringProperty("Category", x))
            ]
        );
    }

    private bool UnhandledFilter(ITestExecutionFilter testExecutionFilter)
    {
        _logger.LogWarning($"Filter is Unhandled Type: {testExecutionFilter.GetType().FullName}");
        return true;
    }
}
