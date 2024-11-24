#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Assertions.Primitives;

public static class CharIsExtensions
{
    public static InvokableValueAssertionBuilder<char> IsEqualTo(this IValueSource<char> valueSource, char expected)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<char>(expected)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<char?> IsEqualTo(this IValueSource<char?> valueSource, char expected)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<char?>(expected)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<char?> IsEqualTo(this IValueSource<char?> valueSource, char? expected)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<char?>(expected)
            , []);
    }
}