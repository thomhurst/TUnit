namespace TUnit.Assertions.AssertConditions.Generic;

public class TypeOfExpectedValueAssertCondition<TActual>(Type expectedType)
    : BaseAssertCondition<TActual>
{
    protected internal override string GetFailureMessage() => $"{ActualValue} is {ActualValue?.GetType().Name ?? "null"} instead of {expectedType.Name}";

    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue?.GetType() == expectedType;
    }
}