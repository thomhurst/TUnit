namespace TUnit.Core;

// Don't think there's a way to enable inheritance on this because the source generator needs to access the constructor argument
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public sealed class RepeatAttribute : TUnitAttribute, IScopedAttribute<RepeatAttribute>
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
