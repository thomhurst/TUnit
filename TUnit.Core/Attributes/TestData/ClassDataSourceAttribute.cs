using BindingFlags = System.Reflection.BindingFlags;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class ClassDataSourceAttribute<T>() : ClassDataSourceAttribute(typeof(T)) where T : new(); 

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class ClassDataSourceAttribute : TUnitAttribute
{
    public Type Type { get; }

    public ClassDataSourceAttribute(Type type)
    {
        if (type.GetConstructors().First().GetParameters().Any())
        {
            throw new ArgumentException($"{type.FullName} cannot be used within [ClassData] as it does not have a parameterless constructor.");
        }

        if (!type.GetConstructors().First().IsPublic)
        {
            throw new ArgumentException($"{type.FullName} cannot be used within [ClassData] as it does not have a public constructor.");
        }
        
        Type = type;
    }
    
    public SharedType Shared { get; set; } = SharedType.None;
    public string? Key { get; set; }
}

public enum SharedType
{
    None,
    ForClass,
    Globally,
    Keyed,
}