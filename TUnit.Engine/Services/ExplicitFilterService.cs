using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

#pragma warning disable TPEXP

namespace TUnit.Engine.Services;

internal class ExplicitFilterService
{
    public bool CanRun(TestInformation testInformation, ITestExecutionFilter? filter)
    {
        if (!testInformation.TestAndClassAttributes.Any(x => x is ExplicitAttribute))
        {
            // These tests don't have any ExplicitAttributes
            return true;
        }

        if (filter is null or NopFilter)
        {
            return false;
        }

        // Filters have already done matching - And the filter is not null due to the above check
        // So we've explicitly filtered these tests!
        return true;
    }
    
    
}