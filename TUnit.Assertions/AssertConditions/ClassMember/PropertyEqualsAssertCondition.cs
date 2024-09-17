using System.Linq.Expressions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.ClassMember;

public class PropertyEqualsAssertCondition<TRootObjectType, TPropertyType>(Expression<Func<TRootObjectType, TPropertyType>> propertySelector, TPropertyType expected, bool isEqual)
    : AssertCondition<TRootObjectType, TPropertyType>(expected)
{
    protected override string DefaultMessage => $"""
                                                 {typeof(TRootObjectType).Name}.{ExpressionHelpers.GetName(propertySelector)}:
                                                     Expected: {ExpectedValue}
                                                     Received: { GetPropertyValue(ActualValue)?.ToString() ?? $"Object `{typeof(TRootObjectType).Name}` was null" }
                                                 """;

    protected internal override bool Passes(TRootObjectType? actualValue, Exception? exception,
        string? rawValueExpression)
    {
        var propertyValue = GetPropertyValue(actualValue);
        
        return Equals(propertyValue, ExpectedValue) == isEqual;
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