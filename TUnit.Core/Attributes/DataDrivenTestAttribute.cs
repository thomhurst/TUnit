namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class DataDrivenTestAttribute : TUnitAttribute
{
    public object?[] Values { get; }

    public DataDrivenTestAttribute(params object?[]? values)
    {
        Values = values ?? [null];
    }
}