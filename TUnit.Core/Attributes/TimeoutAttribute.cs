namespace TUnit.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class TimeoutAttribute : TUnitAttribute
{
    public TimeSpan Timeout { get; }
    public TimeoutAttribute(int timeoutInMilliseconds)
    {
        Timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);
    }
}