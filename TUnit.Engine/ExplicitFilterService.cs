using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

namespace TUnit.Engine;

public class ExplicitFilterService
{
    public bool CanRun(TestInformation testInformation, ITestExecutionFilter? filter)
    {
        if (!testInformation.LazyTestAndClassAttributes
                .Value.Any(x => x is ExplicitAttribute))
        {
            return true;
        }

        if (filter is null)
        {
            return false;
        }

        if (filter is TestNodeUidListFilter testNodeUidListFilter
            && testNodeUidListFilter.TestNodeUids.Contains(new TestNodeUid(testInformation.TestId)))
        {
            return true;
        }

        return false;
    }
    
    
}