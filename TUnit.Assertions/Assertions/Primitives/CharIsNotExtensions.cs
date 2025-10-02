#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.Extensions;

public static class CharIsNotExtensions
{
    public static InvokableValueAssertion<char> IsNotEqualTo(this IValueSource<char> valueSource, char expected)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<char>(expected)
            , []);
    }

    public static InvokableValueAssertion<char?> IsNotEqualTo(this IValueSource<char?> valueSource, char expected)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<char?>(expected)
            , []);
    }

    public static InvokableValueAssertion<char?> IsNotEqualTo(this IValueSource<char?> valueSource, char? expected)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<char?>(expected)
            , []);
    }
}
