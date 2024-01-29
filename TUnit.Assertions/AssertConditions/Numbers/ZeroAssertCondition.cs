using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class ZeroAssertCondition<TActual> : AssertCondition<TActual, TActual> 
    where TActual : INumber<TActual>, IEqualityOperators<TActual, TActual, bool>
{
    public ZeroAssertCondition(TActual? expected) : base(expected)
    {
    }

    public override string DefaultMessage => $"{ActualValue} is not equal to 0";

    protected internal override bool Passes(TActual actualValue)
    {
        return actualValue == TActual.Zero;
    }
}