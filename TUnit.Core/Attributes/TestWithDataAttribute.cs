namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TestWithDataAttribute : TUnitAttribute
{
    public object?[] Values { get; }

    public TestWithDataAttribute(params object?[] values)
    {
        Values = values;
    }
}