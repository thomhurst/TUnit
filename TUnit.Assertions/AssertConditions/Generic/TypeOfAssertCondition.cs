namespace TUnit.Assertions.AssertConditions.Generic;

public class TypeOfAssertCondition<TActual>(Type expectedType)
    : AssertCondition<TActual, object?>(default)
{
    protected internal override string GetFailureMessage() => $"{ActualValue} is {ActualValue?.GetType().Name ?? "null"} instead of {expectedType.Name}";

    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue?.GetType() == expectedType;
    }
}