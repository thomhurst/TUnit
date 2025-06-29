namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ArgumentsAttribute : TestDataAttribute
{
    public object?[] Values { get; }

    public ArgumentsAttribute()
    {
        if (Values == null)
        {
            throw new ArgumentNullException(nameof(Values), "No arguments were provided");
        }
    }

    public ArgumentsAttribute(params object?[]? values)
    {
        Values = values ?? [null];
    }
}
