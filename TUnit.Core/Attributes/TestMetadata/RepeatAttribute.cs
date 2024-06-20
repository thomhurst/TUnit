namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RepeatAttribute : TUnitAttribute
{
    public int Times { get; }

    public RepeatAttribute(int times)
    {
        if (times < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(times), "Repeat times must be positive");
        }
        
        Times = times;
    }
}