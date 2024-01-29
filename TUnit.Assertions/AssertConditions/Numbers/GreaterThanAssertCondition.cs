using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class GreaterThanAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected> 
    where TExpected : INumber<TExpected>
    where TActual : INumber<TActual>, TExpected
{
    public GreaterThanAssertCondition(IReadOnlyCollection<AssertCondition<TActual, TExpected>> nestedAssertConditions, NestedConditionsOperator? nestedConditionsOperator, TExpected? expected) : base(nestedAssertConditions, nestedConditionsOperator, expected)
    {
    }

    public override string DefaultMessage => $"{ActualValue} is not greater than {ExpectedValue}";

    protected override bool Passes(TActual actualValue)
    {
        return actualValue > ExpectedValue!;
    }
}