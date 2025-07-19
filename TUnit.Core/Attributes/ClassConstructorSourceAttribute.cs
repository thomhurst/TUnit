using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

// Base abstract class
public abstract class BaseClassConstructorAttribute : TUnitAttribute
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public abstract Type ClassConstructorType { get; init; }

    private protected BaseClassConstructorAttribute() { }

    private protected BaseClassConstructorAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type classType)
    {
        ClassConstructorType = classType;
    }
}

// Single sealed attribute with both generic and non-generic constructors
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public class ClassConstructorAttribute : BaseClassConstructorAttribute
{
    public ClassConstructorAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type classConstructorType)
        : base(classConstructorType)
    {
        ClassConstructorType = classConstructorType;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public override Type ClassConstructorType { get; init; }
}

// Generic version for C#
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public sealed class ClassConstructorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] T>()
    : ClassConstructorAttribute(typeof(T))
    where T : IClassConstructor, new()
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public override Type ClassConstructorType { get; init; } = typeof(T);
}
