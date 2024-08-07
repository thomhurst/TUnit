namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class RetryAttribute : TUnitAttribute
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
}