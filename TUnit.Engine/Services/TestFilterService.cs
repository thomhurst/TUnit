using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Exceptions;

namespace TUnit.Engine.Services;

internal class TestFilterService
{
    private readonly ILogger<TestFilterService> _logger;

    public TestFilterService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TestFilterService>();
    }
    
    public ParallelQuery<DiscoveredTest> FilterTests(ITestExecutionFilter? testExecutionFilter, ParallelQuery<DiscoveredTest> testNodes)
    {
#pragma warning disable TPEXP
        if (testExecutionFilter is null or NopFilter)
#pragma warning restore TPEXP
        {
            return testNodes;
        }

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
        return
            $"/{testDetails.ClassType.Assembly.FullName}/{testDetails.ClassType.Namespace}/{testDetails.ClassType.Name}/{testDetails.MethodInfo.Name}";
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