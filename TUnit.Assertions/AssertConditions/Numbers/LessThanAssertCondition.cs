using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class LessThanAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected> 
    where TExpected : INumber<TExpected>
    where TActual : INumber<TActual>, TExpected
{
    public LessThanAssertCondition(IReadOnlyCollection<AssertCondition<TActual, TExpected>> nestedAssertConditions, NestedConditionsOperator? nestedConditionsOperator, TExpected? expected) : base(nestedAssertConditions, nestedConditionsOperator, expected)
    {
    }

    public override string DefaultMessage => $"{ActualValue} is not less than {ExpectedValue}";

    protected override bool Passes(TActual actualValue)
    {
        return actualValue < ExpectedValue!;
    }
}