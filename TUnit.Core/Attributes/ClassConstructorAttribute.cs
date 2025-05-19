using System;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

// Base abstract class
public abstract class BaseClassConstructorAttribute : TUnitAttribute, IDataAttribute
{
    public abstract Type ClassConstructorType { get; set; }

    internal BaseClassConstructorAttribute() { }
    internal BaseClassConstructorAttribute(Type classType) { ClassConstructorType = classType; }
}

// Single sealed attribute with both generic and non-generic constructors
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public partial class ClassConstructorAttribute : BaseClassConstructorAttribute
{
    public ClassConstructorAttribute(Type classConstructorType)
        : base(classConstructorType)
    {
        ClassConstructorType = classConstructorType;
    }

    public override Type ClassConstructorType { get; set; }
}

// Generic version for C#
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public sealed class ClassConstructorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    : ClassConstructorAttribute(typeof(T))
    where T : IClassConstructor, new()
{
    public override Type ClassConstructorType { get; set; } = typeof(T);
}
