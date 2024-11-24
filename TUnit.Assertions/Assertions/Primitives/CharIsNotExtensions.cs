#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Assertions.Primitives;

public static class CharIsNotExtensions
{
    public static InvokableValueAssertionBuilder<char> IsNotEqualTo(this IValueSource<char> valueSource, char expected)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<char>(expected)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<char?> IsNotEqualTo(this IValueSource<char?> valueSource, char expected)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<char?>(expected)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<char?> IsNotEqualTo(this IValueSource<char?> valueSource, char? expected)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<char?>(expected)
            , []);
    }
}