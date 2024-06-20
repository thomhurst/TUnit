namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ArgumentsAttribute : TUnitAttribute
{
    public object?[] Values { get; }

    public ArgumentsAttribute()
    {
        ArgumentNullException.ThrowIfNull(Values);
    }

    public ArgumentsAttribute(params object?[]? values)
    {
        Values = values ?? [null];
    }
}