namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ClassDataAttribute : TUnitAttribute
{
    public Type Type { get; }

    public ClassDataAttribute(Type type)
    {
        Type = type;
    }
}