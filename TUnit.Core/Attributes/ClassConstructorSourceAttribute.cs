using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public class ClassConstructorAttribute : TUnitAttribute
{
    public ClassConstructorAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type classConstructorType)
    {
        ClassConstructorType = classConstructorType;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public Type ClassConstructorType { get; init; }
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public sealed class ClassConstructorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] T>()
    : ClassConstructorAttribute(typeof(T))
    where T : IClassConstructor, new();
