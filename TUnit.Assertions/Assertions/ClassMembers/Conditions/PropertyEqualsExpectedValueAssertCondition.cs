using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.ClassMember;

public class PropertyEqualsExpectedValueAssertCondition<TRootObjectType, TPropertyType>(Expression<Func<TRootObjectType, TPropertyType>> propertySelector, TPropertyType expected, bool isEqual)
    : ExpectedValueAssertCondition<TRootObjectType, TPropertyType>(expected)
{
    internal protected override string GetExpectation()
    {
        return $"{typeof(TRootObjectType).Name}.{ExpressionHelpers.GetName(propertySelector)} to be equal to {ExpectedValue}";
    }

#pragma warning disable IL2046 // Member with 'RequiresUnreferencedCodeAttribute' overrides base member without 'RequiresUnreferencedCodeAttribute'
#pragma warning disable IL3051 // Member with 'RequiresDynamicCodeAttribute' overrides base member without 'RequiresDynamicCodeAttribute'
    [RequiresUnreferencedCode("Expression compilation requires unreferenced code")]
    [RequiresDynamicCode("Expression compilation requires dynamic code generation")]
    protected override ValueTask<AssertionResult> GetResult(TRootObjectType? actualValue, TPropertyType? expectedValue)
    {
        var propertyValue = GetPropertyValue(actualValue);
        return AssertionResult
            .FailIf(actualValue is null,
                $"Object `{typeof(TRootObjectType).Name}` was null")
            .OrFailIf(Equals(propertyValue, expectedValue) != isEqual,
                $"received {GetPropertyValue(actualValue)?.ToString()}"
            );
    }
#pragma warning restore IL3051
#pragma warning restore IL2046

    [RequiresUnreferencedCode("Expression compilation requires unreferenced code")]
    [RequiresDynamicCode("Expression compilation requires dynamic code generation")]
    private object? GetPropertyValue(TRootObjectType? actualValue)
    {
        if (actualValue is null)
        {
            return null;
        }

        return propertySelector.Compile()(actualValue);
    }
}
