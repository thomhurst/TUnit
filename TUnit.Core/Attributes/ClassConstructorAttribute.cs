using TUnit.Core;
using TUnit.Core.Interfaces;

public abstract class ClassConstructorAttribute : TUnitAttribute
{
    public abstract Type ClassConstructorType { get; }

    internal ClassConstructorAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public sealed class ClassConstructorAttribute<T> : ClassConstructorAttribute where T : IClassConstructor, new()
{
    public override Type ClassConstructorType { get; } = typeof(T);
}