using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class IsOddAssertCondition<TActual> : AssertCondition<TActual, TActual>
    where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
{
    public IsOddAssertCondition(IReadOnlyCollection<AssertCondition<TActual, TActual>> nestedAssertConditions, NestedConditionsOperator? nestedConditionsOperator, TActual? expected) : base(nestedAssertConditions, nestedConditionsOperator, expected)
    {
    }

    public override string DefaultMessage => $"{ActualValue} is not odd";

    protected override bool Passes(TActual actualValue)
    {
        return actualValue % 2 != 0;
    }
}