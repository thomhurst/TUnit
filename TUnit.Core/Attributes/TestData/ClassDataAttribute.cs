namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class ClassDataAttribute : TUnitAttribute
{
    public Type Type { get; }

    public ClassDataAttribute(Type type)
    {
        if (type.GetConstructors().First().GetParameters().Any())
        {
            throw new ArgumentException($"{type.FullName} cannot be used within [ClassData] as it does not have a parameterless constructor.");
        }
        
        Type = type;
    }
}