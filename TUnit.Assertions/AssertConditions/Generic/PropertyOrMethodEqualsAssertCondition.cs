using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Generic;

public class PropertyOrMethodEqualsAssertCondition<TExpected>(string propertyName, TExpected expected)
    : AssertCondition<object, TExpected>(expected)
{
    public override string DefaultMessage => $"Expected {ExpectedValue} but received {ActualValue}";

    protected internal override bool Passes(object actualValue)
    {
        var propertyValue = GetPropertyValue(actualValue);

        WithMessage(_ => $"Expected {ExpectedValue} but received {propertyValue}");
        
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