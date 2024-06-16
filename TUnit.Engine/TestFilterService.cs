using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Properties;

namespace TUnit.Engine;

internal class TestFilterService
{
    private readonly ILogger<TestFilterService> _logger;

    public TestFilterService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TestFilterService>();
    }
    
    public IEnumerable<DiscoveredTest> FilterTests(ITestExecutionFilter? testExecutionFilter, IEnumerable<DiscoveredTest> testNodes)
    {
        if (testExecutionFilter is null)
        {
            return testNodes;
        }

        return testNodes.Where(x => MatchesTest(testExecutionFilter, x.TestInformation));
    }

    public bool MatchesTest(ITestExecutionFilter? testExecutionFilter, TestInformation testInformation)
    {
#pragma warning disable TPEXP
        return testExecutionFilter switch
        {
            null => true,
            NopFilter => true,
            TestNodeUidListFilter testNodeUidListFilter => testNodeUidListFilter.TestNodeUids.Contains(new TestNodeUid(testInformation.TestId)),
            TreeNodeFilter treeNodeFilter => treeNodeFilter.MatchesFilter(BuildPath(testInformation), BuildPropertyBag(testInformation)),
            _ => UnhandledFilter(testExecutionFilter)
        };
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
                ..testInformation.Categories.Select(x => new CategoryProperty(x))
            ]
        );
    }

    private bool UnhandledFilter(ITestExecutionFilter testExecutionFilter)
    {
        _logger.LogWarning($"Filter is Unhandled Type: {testExecutionFilter.GetType().FullName}");
        return true;
    }
}