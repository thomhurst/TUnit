using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Generic;

public class PropertyEqualsAssertCondition<TActual, TExpected>(string propertyName, TExpected expected)
    : AssertCondition<TActual, TExpected>(expected)
{
    protected override string DefaultMessage => $"Expected {ExpectedValue} but received {ActualValue}";

    protected internal override bool Passes(TActual? actualValue)
    {
        var propertyValue = GetPropertyValue(actualValue);
        
        WithMessage(_ => $"Expected {ExpectedValue} but received {propertyValue}");
        
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