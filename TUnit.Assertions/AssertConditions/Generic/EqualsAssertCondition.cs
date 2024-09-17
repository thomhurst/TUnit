namespace TUnit.Assertions.AssertConditions.Generic;

public class EqualsAssertCondition<TActual>(TActual expected) : AssertCondition<TActual, TActual>(expected)
{
    protected internal override string GetFailureMessage() => $"""
                                                 Expected: {ExpectedValue}
                                                 Received: {ActualValue}
                                                 """;

    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is IEquatable<TActual> equatable)
        {
            return equatable.Equals(ExpectedValue);
        }
        
        return Equals(actualValue, ExpectedValue);
    }
}