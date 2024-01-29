using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class IsEvenAssertCondition<TActual> : AssertCondition<TActual, TActual>
    where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
{
    public IsEvenAssertCondition(IReadOnlyCollection<AssertCondition<TActual, TActual>> nestedAssertConditions, NestedConditionsOperator? nestedConditionsOperator, TActual? expected) : base(nestedAssertConditions, nestedConditionsOperator, expected)
    {
    }

    public override string DefaultMessage => $"{ActualValue} is not even";

    protected override bool Passes(TActual actualValue)
    {
        return actualValue % 2 == 0;
    }
}