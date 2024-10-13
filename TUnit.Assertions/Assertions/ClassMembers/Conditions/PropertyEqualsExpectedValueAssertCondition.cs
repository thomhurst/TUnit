using System.Linq.Expressions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.ClassMember;

public class PropertyEqualsExpectedValueAssertCondition<TRootObjectType, TPropertyType>(Expression<Func<TRootObjectType, TPropertyType>> propertySelector, TPropertyType expected, bool isEqual)
    : ExpectedValueAssertCondition<TRootObjectType, TPropertyType>(expected)
{
    protected override string GetExpectation()
    {
        return $"{typeof(TRootObjectType).Name}.{ExpressionHelpers.GetName(propertySelector)} to be equal to {expected}";
    }

    protected override AssertionResult GetResult(TRootObjectType? actualValue, TPropertyType? expectedValue)
    {
        var propertyValue = GetPropertyValue(actualValue);
        return AssertionResult
            .FailIf(
                () => actualValue is null,
                $"Object `{typeof(TRootObjectType).Name}` was null")
            .OrFailIf(
                () => Equals(propertyValue, expectedValue) != isEqual,
                $"received {GetPropertyValue(actualValue)?.ToString()}"
            );
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