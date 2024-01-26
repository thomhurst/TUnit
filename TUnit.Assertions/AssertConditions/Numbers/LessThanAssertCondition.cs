using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class LessThanAssertCondition<TActual, TExpected> : ExpectedValueAssertCondition<TActual, TExpected> 
    where TExpected : INumber<TExpected>
    where TActual : INumber<TActual>, TExpected
{
    public LessThanAssertCondition(TExpected expected) : base(expected)
    {
    }

    public override string DefaultMessage => $"{ActualValue} is not less than {ExpectedValue}";
    
    protected override bool Passes(TActual actualValue)
    {
        return actualValue < ExpectedValue;
    }
}