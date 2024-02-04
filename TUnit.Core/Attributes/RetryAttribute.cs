namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
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
}