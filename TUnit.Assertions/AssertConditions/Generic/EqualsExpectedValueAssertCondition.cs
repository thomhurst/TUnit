namespace TUnit.Assertions.AssertConditions.Generic;

public class EqualsExpectedValueAssertCondition<TActual>(TActual expected) : ExpectedValueAssertCondition<TActual, TActual>(expected)
{
    protected override string GetFailureMessage(TActual? actualValue, TActual? expectedValue) => $"""
                                                 Expected: {ExpectedValue}
                                                 Received: {ActualValue}
                                                 """;

    protected override AssertionResult Passes(TActual? actualValue, TActual? expectedValue)
    {
        if (actualValue is IEquatable<TActual> equatable)
        {
            return equatable.Equals(ExpectedValue);
        }
        
        return Equals(actualValue, ExpectedValue);
    }
}