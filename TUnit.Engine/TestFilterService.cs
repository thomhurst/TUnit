using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;

namespace TUnit.Engine;

public class TestFilterService
{
    public IEnumerable<TestNode> FilterTests(ITestExecutionFilter? testExecutionFilter, List<TestNode> testNodes)
    {
        if (testExecutionFilter is null)
        {
            return testNodes;
        }

        return testNodes.Where(x => MatchesTest(testExecutionFilter, x));
    }

    public bool MatchesTest(ITestExecutionFilter? testExecutionFilter, TestNode testNode)
    {
        switch (testExecutionFilter)
        {
            case null:
                return true;
            case TestNodeUidListFilter testNodeUidListFilter:
                return testNodeUidListFilter.TestNodeUids.Contains(testNode.Uid);
            case BasicFilter basicFilter:
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(testExecutionFilter));
        }
    }
}