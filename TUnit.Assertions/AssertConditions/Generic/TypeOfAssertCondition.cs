using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Generic;

public class TypeOfAssertCondition<TActual, TAnd, TOr>(Type expectedType)
    : AssertCondition<TActual, object?, TAnd, TOr>(default)
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected override string DefaultMessage => $"{ActualValue} is {ActualValue?.GetType().Name ?? "null"} instead of {expectedType.Name}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        return actualValue?.GetType() == expectedType;
    }
}