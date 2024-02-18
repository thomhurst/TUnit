using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Generic;

public class PropertyEqualsAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder<TActual> assertionBuilder, string propertyName, TExpected expected)
    : AssertCondition<TActual, TExpected, TAnd, TOr>(assertionBuilder,  expected)
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected override string DefaultMessage => $"""
                                                 Expected: {ExpectedValue}
                                                 Received: {ActualValue}
                                                 """;

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        var propertyValue = GetPropertyValue(actualValue);

        WithMessage((_, _) => $"""
                               Expected: {ExpectedValue}
                               Received: {propertyValue}
                               """);
        
        return Equals(propertyValue, ExpectedValue);
    }

    private object? GetPropertyValue(object? actualValue)
    { 
        ArgumentNullException.ThrowIfNull(actualValue);
        
        if (actualValue.GetType().GetProperty(propertyName) is null)
        {
            throw new ArgumentException($"No {propertyName} property or method was found on {actualValue.GetType().Name}");
        }
        
        return actualValue.GetPropertyValue(propertyName);
    }
}