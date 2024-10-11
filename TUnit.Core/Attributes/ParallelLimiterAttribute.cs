using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ParallelLimiterAttribute<TParallelLimit>() : ParallelLimiterAttribute(typeof(TParallelLimit))
    where TParallelLimit : IParallelLimit, new();

public class ParallelLimiterAttribute : TUnitAttribute
{
    public Type Type { get; }

    internal ParallelLimiterAttribute(Type type)
    {
        Type = type;
    }
}