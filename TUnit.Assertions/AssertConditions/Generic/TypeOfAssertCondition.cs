namespace TUnit.Assertions.AssertConditions.Generic;

public class TypeOfAssertCondition<TActual>(Type expectedType)
    : AssertCondition<TActual, object?>(default)
{
    protected override string DefaultMessage => $"{ActualValue} is {ActualValue?.GetType().Name ?? "null"} instead of {expectedType.Name}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        return actualValue?.GetType() == expectedType;
    }
}