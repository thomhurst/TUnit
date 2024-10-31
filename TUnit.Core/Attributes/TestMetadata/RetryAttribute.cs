using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class RetryAttribute : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public int Times { get; }

    public RetryAttribute(int times)
    {
        if (times < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(times), "Retry times must be positive");
        }

        Times = times;
    }

    public virtual Task<bool> ShouldRetry(TestContext context, Exception exception, int currentRetryCount)
    {
        return Task.FromResult(true);
    }

    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.SetRetryCount(Times, ShouldRetry);
    }
}