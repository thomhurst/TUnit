using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

namespace TUnit.Engine;

public class TestFilterService
{
    public IEnumerable<TestInformation> FilterTests(ITestExecutionFilter? testExecutionFilter, List<TestInformation> testNodes)
    {
        if (testExecutionFilter is null)
        {
            return testNodes;
        }

        return testNodes.Where(x => MatchesTest(testExecutionFilter, x));
    }

    public bool MatchesTest(ITestExecutionFilter? testExecutionFilter, TestInformation testInformation)
    {
        return testExecutionFilter switch
        {
            null => true,
            TestNodeUidListFilter testNodeUidListFilter => testNodeUidListFilter.TestNodeUids.Contains(new TestNodeUid(testInformation.TestId)),
            BasicFilter basicFilter => true,
            _ => throw new ArgumentOutOfRangeException(nameof(testExecutionFilter))
        };
    }
}