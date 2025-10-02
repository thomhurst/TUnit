#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.Extensions;

public static class BooleanIsExtensions
{
    public static InvokableValueAssertion<bool> IsEqualTo(this IValueSource<bool> valueSource, bool expected)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<bool>(expected)
            , []);
    }

    public static InvokableValueAssertion<bool> IsTrue(this IValueSource<bool> valueSource)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<bool>(true)
            , []);
    }

    public static InvokableValueAssertion<bool> IsFalse(this IValueSource<bool> valueSource)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<bool>(false)
            , []);
    }

    public static InvokableValueAssertion<bool?> IsEqualTo(this IValueSource<bool?> valueSource, bool expected)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<bool?>(expected)
            , []);
    }

    public static InvokableValueAssertion<bool?> IsEqualTo(this IValueSource<bool?> valueSource, bool? expected)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<bool?>(expected)
            , []);
    }

    public static InvokableValueAssertion<bool?> IsTrue(this IValueSource<bool?> valueSource)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<bool?>(true)
            , []);
    }

    public static InvokableValueAssertion<bool?> IsFalse(this IValueSource<bool?> valueSource)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<bool?>(false)
            , []);
    }
}
