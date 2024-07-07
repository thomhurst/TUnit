using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Models;
using TUnit.Engine.Properties;

namespace TUnit.Engine.Services;

internal class TestFilterService
{
    private readonly ILogger<TestFilterService> _logger;

    public TestFilterService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TestFilterService>();
    }
    
    public IEnumerable<DiscoveredTest> FilterTests(ITestExecutionFilter? testExecutionFilter, IEnumerable<DiscoveredTest> testNodes)
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
            TestNodeUidListFilter testNodeUidListFilter => testNodeUidListFilter.TestNodeUids.Contains(new TestNodeUid(discoveredTest.TestInformation.TestId)),
            TreeNodeFilter treeNodeFilter => treeNodeFilter.MatchesFilter(BuildPath(discoveredTest.TestInformation), BuildPropertyBag(discoveredTest.TestInformation)),
            _ => UnhandledFilter(testExecutionFilter)
        };

        if (!shouldRunTest)
        {
            discoveredTest.TestContext._taskCompletionSource.SetException(new TestNotExecutedException(discoveredTest.TestInformation));
        }

        return shouldRunTest;
#pragma warning restore TPEXP
    }

    private string BuildPath(TestInformation testInformation)
    {
        return
            $"/{testInformation.ClassType.Assembly.FullName}/{testInformation.ClassType.Namespace}/{testInformation.ClassType.Name}/{testInformation.MethodInfo.Name}";
    }

    private PropertyBag BuildPropertyBag(TestInformation testInformation)
    {
        return new PropertyBag(
            [
                ..testInformation.CustomProperties.Select(x => new KeyValuePairStringProperty(x.Key, x.Value)),
                ..testInformation.Categories.Select(x => new KeyValuePairStringProperty("Category", x))
            ]
        );
    }

    private bool UnhandledFilter(ITestExecutionFilter testExecutionFilter)
    {
        _logger.LogWarning($"Filter is Unhandled Type: {testExecutionFilter.GetType().FullName}");
        return true;
    }
}