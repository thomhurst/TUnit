using System.Linq.Expressions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.ClassMember;

public class PropertyEqualsAssertCondition<TRootObjectType, TPropertyType, TAnd, TOr>(AssertionBuilder<TRootObjectType> assertionBuilder, Expression<Func<TRootObjectType, TPropertyType>> propertySelector, TPropertyType expected, bool isEqual)
    : AssertCondition<TRootObjectType, TPropertyType, TAnd, TOr>(assertionBuilder,  expected)
    where TAnd : And<TRootObjectType, TAnd, TOr>, IAnd<TAnd, TRootObjectType, TAnd, TOr>
    where TOr : Or<TRootObjectType, TAnd, TOr>, IOr<TOr, TRootObjectType, TAnd, TOr>
{
    protected override string DefaultMessage => $"""
                                                 {typeof(TRootObjectType).Name}.{ExpressionHelpers.GetName(propertySelector)}:
                                                     Expected: {ExpectedValue}
                                                     Received: { GetPropertyValue(ActualValue)?.ToString() ?? $"Object `{typeof(TRootObjectType).Name}` was null" }
                                                 """;

    protected internal override bool Passes(TRootObjectType? actualValue, Exception? exception)
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