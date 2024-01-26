using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class LessThanOrEqualToAssertCondition<TActual, TExpected> : ExpectedValueAssertCondition<TActual, TExpected> 
    where TExpected : INumber<TExpected>
    where TActual : INumber<TActual>, TExpected
{
    public LessThanOrEqualToAssertCondition(TExpected expected) : base(expected)
    {
    }

    public override string DefaultMessage => $"{ActualValue} is not less than or equal to {ExpectedValue}";
    
    protected override bool Passes(TActual actualValue)
    {
        return actualValue <= ExpectedValue;
    }
}