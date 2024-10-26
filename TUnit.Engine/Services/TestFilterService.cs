using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Exceptions;

namespace TUnit.Engine.Services;

internal class TestFilterService(ILoggerFactory loggerFactory)
{
    private readonly ILogger<TestFilterService> _logger = loggerFactory.CreateLogger<TestFilterService>();

    public IEnumerable<DiscoveredTest> FilterTests(ITestExecutionFilter? testExecutionFilter, IEnumerable<DiscoveredTest> testNodes)
    {
#pragma warning disable TPEXP
        if (testExecutionFilter is null or NopFilter)
#pragma warning restore TPEXP
        {
            _logger.LogTrace("No test filter found.");
            
            return testNodes;
        }
        
        _logger.LogTrace($"Test filter is: {testExecutionFilter?.GetType().Name ?? "null"}");

        return testNodes.Where(x => MatchesTest(testExecutionFilter, x));
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

        if (!shouldRunTest)
        {
            discoveredTest.TestContext.TaskCompletionSource.SetException(new TestNotExecutedException(discoveredTest.TestDetails));
        }

        return shouldRunTest;
#pragma warning restore TPEXP
    }

    private string BuildPath(TestDetails testDetails)
    {
        var assembly = testDetails.ClassType.Assembly.GetName();

        var classTypeName = testDetails.ClassType.Name;
        
        return
            $"/{assembly.Name ?? assembly.FullName}/{testDetails.ClassType.Namespace}/{classTypeName}/{testDetails.MethodInfo.Name}";
    }

    private PropertyBag BuildPropertyBag(TestDetails testDetails)
    {
        return new PropertyBag(
            [
                ..testDetails.CustomProperties.Select(x => new KeyValuePairStringProperty(x.Key, x.Value)),
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