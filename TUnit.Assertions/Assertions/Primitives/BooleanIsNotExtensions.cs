#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.Extensions;

public static class BooleanIsNotExtensions
{
    public static InvokableValueAssertionBuilder<bool> IsNotEqualTo(this IValueSource<bool> valueSource, bool expected)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<bool>(expected)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<bool> IsNotTrue(this IValueSource<bool> valueSource)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<bool>(true)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<bool> IsNotFalse(this IValueSource<bool> valueSource)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<bool>(false)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<bool?> IsNotEqualTo(this IValueSource<bool?> valueSource, bool expected)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<bool?>(expected)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<bool?> IsNotEqualTo(this IValueSource<bool?> valueSource, bool? expected)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<bool?>(expected)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<bool?> IsNotTrue(this IValueSource<bool?> valueSource)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<bool?>(true)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<bool?> IsNotFalse(this IValueSource<bool?> valueSource)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<bool?>(false)
            , []);
    }
}