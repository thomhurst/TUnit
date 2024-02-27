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
        if (testExecutionFilter is BasicFilter basicFilter)
        {
            return basicFilter.MatchesFilter(testNode);
        }

        return true;
    }
}