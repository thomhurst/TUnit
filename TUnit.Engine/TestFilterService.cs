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
        return testExecutionFilter switch
        {
            null => true,
            TestNodeUidListFilter testNodeUidListFilter => testNodeUidListFilter.TestNodeUids.Contains(testNode.Uid),
            BasicFilter basicFilter => true,
            _ => throw new ArgumentOutOfRangeException(nameof(testExecutionFilter))
        };
    }
}