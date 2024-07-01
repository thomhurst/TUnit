using System.Net;

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

    public virtual Task<bool> ShouldRetry(TestInformation testInformation, Exception exception, int currentRetryCount)
    {
        return Task.FromResult(true);
    }
}

public class RetryTransientHttpAttribute : RetryAttribute
{
    public RetryTransientHttpAttribute(int times) : base(times)
    {
    }

    public override Task<bool> ShouldRetry(TestInformation testInformation, Exception exception, int currentRetryCount)
    {
        if (exception is HttpRequestException requestException)
        {
            return Task.FromResult(requestException.StatusCode is
                HttpStatusCode.BadGateway
                or HttpStatusCode.TooManyRequests
                or HttpStatusCode.GatewayTimeout
                or HttpStatusCode.RequestTimeout);
        }

        return Task.FromResult(false);
    }
}