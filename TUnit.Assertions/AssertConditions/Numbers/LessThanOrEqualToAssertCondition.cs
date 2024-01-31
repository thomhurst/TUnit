using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class LessThanOrEqualToAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected> 
    where TExpected : INumber<TExpected>
    where TActual : INumber<TActual>, TExpected
{
    public LessThanOrEqualToAssertCondition(TExpected? expected) : base(expected)
    {
    }

    protected override string DefaultMessage => $"{ActualValue} is not less than or equal to {ExpectedValue}";

    protected internal override bool Passes(TActual? actualValue)
    {
        ArgumentNullException.ThrowIfNull(actualValue);
        
        return actualValue <= ExpectedValue!;
    }
}