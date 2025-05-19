using TUnit.Core.Enums;

namespace TUnit.Core;

public class AfterTestContext
{
    internal readonly DiscoveredTest DiscoveredTest;

    internal AfterTestContext(DiscoveredTest discoveredTest)
    {
        DiscoveredTest = discoveredTest;
    }
    
    public TestContext TestContext => DiscoveredTest.TestContext;
    public TestDetails TestDetails => TestContext.TestDetails;
    
    public void OverrideResult(Status status, string reason)
    {
        var testResult = TestContext.Result;

        if (testResult is null)
        {
            throw new InvalidOperationException("There is no test result to override.");
        }
        
        OverrideResult(testResult with
        {
            Status = status,
            IsOverridden = true,
            OverrideReason = reason
        });

        if(status == Status.Skipped)
        {
            TestContext.SkipReason = reason;
        }
    }
    
    public void OverrideResult(Exception exception, string reason)
    {
        var testResult = TestContext.Result;

        if (testResult is null)
        {
            throw new InvalidOperationException("There is no test result to override.");
        }
        
        OverrideResult(testResult with
        {
            Status = Status.Failed,
            Exception = exception,
            IsOverridden = true,
            OverrideReason = reason,
        });
    }
    
    private void OverrideResult(TestResult result)
    {
        TestContext.Result = result;
    }

    public static implicit operator TestContext(AfterTestContext afterTestContext) => afterTestContext.TestContext;
}