using System.Linq.Expressions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.ClassMember;

public class PropertyEqualsExpectedValueAssertCondition<TRootObjectType, TPropertyType>(Expression<Func<TRootObjectType, TPropertyType>> propertySelector, TPropertyType expected, bool isEqual)
    : ExpectedValueAssertCondition<TRootObjectType, TPropertyType>(expected)
{
    protected override string GetFailureMessage(TRootObjectType? actualValue, TPropertyType? expectedValue) => $"""
         {typeof(TRootObjectType).Name}.{ExpressionHelpers.GetName(propertySelector)}:
             Expected: {expectedValue}
             Received: { GetPropertyValue(actualValue)?.ToString() ?? $"Object `{typeof(TRootObjectType).Name}` was null" }
         """;

    protected override bool Passes(TRootObjectType? actualValue, TPropertyType? expectedValue)
    {
        var propertyValue = GetPropertyValue(actualValue);
        
        return Equals(propertyValue, expectedValue) == isEqual;
    }

    private object? GetPropertyValue(TRootObjectType? actualValue)
    {
        if (actualValue is null)
        {
            return null;
        }

        return propertySelector.Compile()(actualValue);
    }
}