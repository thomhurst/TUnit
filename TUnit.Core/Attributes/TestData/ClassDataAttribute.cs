namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class ClassDataAttribute : TUnitAttribute
{
    public Type Type { get; }

    public ClassDataAttribute(Type type)
    {
        Type = type;
    }
}