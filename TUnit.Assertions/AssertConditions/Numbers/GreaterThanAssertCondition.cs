using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class GreaterThanAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected> 
    where TExpected : INumber<TExpected>
    where TActual : INumber<TActual>, TExpected
{
    public GreaterThanAssertCondition(TExpected? expected) : base(expected)
    {
    }

    public override string DefaultMessage => $"{ActualValue} is not greater than {ExpectedValue}";

    protected internal override bool Passes(TActual? actualValue)
    {
        ArgumentNullException.ThrowIfNull(actualValue);

        return actualValue > ExpectedValue!;
    }
}