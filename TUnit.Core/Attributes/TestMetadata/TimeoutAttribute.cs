namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class TimeoutAttribute(int timeoutInMilliseconds) : TUnitAttribute
{
    public TimeSpan Timeout { get; } = TimeSpan.FromMilliseconds(timeoutInMilliseconds);
}