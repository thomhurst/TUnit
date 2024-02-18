using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Generic;

public class PropertyOrMethodEqualsAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder<TActual> assertionBuilder, string propertyName, TExpected expected)
    : AssertCondition<TActual, TExpected, TAnd, TOr>(assertionBuilder,  expected)
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected override string DefaultMessage => $"Expected {ExpectedValue} but received {ActualValue}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} property {propertyName} is null");
            return false;
        }
        
        var propertyValue = GetPropertyValue(actualValue);

        WithMessage((_, _) => $"Expected {ExpectedValue} but received {propertyValue}");
        
        return Equals(propertyValue, ExpectedValue);
    }

    private object? GetPropertyValue(object actualValue)
    {
        if (actualValue.GetType().GetProperty(propertyName) is null
            && actualValue.GetType().GetProperty(propertyName) is null)
        {
            throw new ArgumentException($"No {propertyName} property or method was found on {actualValue.GetType().Name}");
        }
        
        return actualValue.GetPropertyValue(propertyName) ?? actualValue.GetMethodReturnValue(propertyName);
    }
}