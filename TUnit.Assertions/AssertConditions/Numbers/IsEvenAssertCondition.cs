using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class IsEvenAssertCondition<TActual, TExpected> : ExpectedValueAssertCondition<TActual, TExpected> 
    where TExpected : INumber<TExpected>
    where TActual : INumber<TActual>, TExpected
{
    public IsEvenAssertCondition(TExpected expected) : base(expected)
    {
    }

    public override string DefaultMessage => $"{ActualValue} is not greater than {ExpectedValue}";
    
    protected override bool Passes(TActual actualValue)
    {
        return actualValue > ExpectedValue;
    }
}