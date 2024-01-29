using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class GreaterThanOrEqualToAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected> 
    where TExpected : INumber<TExpected>
    where TActual : INumber<TActual>, TExpected
{
    public GreaterThanOrEqualToAssertCondition(IReadOnlyCollection<AssertCondition<TActual, TExpected>> nestedAssertConditions, TExpected? expected) : base(nestedAssertConditions, expected)
    {
    }

    public override string DefaultMessage => $"{ActualValue} is not greater than or equal to {ExpectedValue}";

    protected override bool Passes(TActual actualValue)
    {
        return actualValue >= ExpectedValue!;
    }
}