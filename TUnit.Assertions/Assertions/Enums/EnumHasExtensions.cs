﻿using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;
using TUnit.Assertions.Assertions.Enums.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Assertions.Enums;

public static class EnumHasExtensions
{
    public static InvokableValueAssertionBuilder<TEnum> HasFlag<TEnum>(this IValueSource<TEnum> valueSource, TEnum expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
    where TEnum : Enum
    {
        return valueSource.RegisterAssertion(new EnumHasFlagAssertCondition<TEnum>(expected),
            [doNotPopulateThisValue1]);
    }
    
    public static InvokableValueAssertionBuilder<TEnum> DoesNotHaveFlag<TEnum>(this IValueSource<TEnum> valueSource, TEnum expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
        where TEnum : Enum
    {
        return valueSource.RegisterAssertion(new EnumDoesNotHaveFlagAssertCondition<TEnum>(expected),
            [doNotPopulateThisValue1]);
    }
    
    public static InvokableValueAssertionBuilder<TEnum> HasSameNameAs<TEnum, TExpected>(this IValueSource<TEnum> valueSource, TExpected expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
        where TEnum : Enum
        where TExpected : Enum
    {
        return valueSource.RegisterAssertion(new EnumHasSameNameAsCondition<TEnum, TExpected>(expected),
            [doNotPopulateThisValue1]);
    }
    
    public static InvokableValueAssertionBuilder<TEnum> DoesNotHaveSameNameAs<TEnum, TExpected>(this IValueSource<TEnum> valueSource, TExpected expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
        where TEnum : Enum
        where TExpected : Enum
    {
        return valueSource.RegisterAssertion(new EnumDoesNotHaveSameNameAsCondition<TEnum, TExpected>(expected),
            [doNotPopulateThisValue1]);
    }
    
    public static InvokableValueAssertionBuilder<TEnum> IsDefined<TEnum>(this IValueSource<TEnum> valueSource)
        where TEnum : Enum
    {
        return valueSource.RegisterAssertion(new EnumIsDefinedAssertCondition<TEnum>(), []);
    }
    
    public static InvokableValueAssertionBuilder<TEnum> IsNotDefined<TEnum>(this IValueSource<TEnum> valueSource)
        where TEnum : Enum
    {
        return valueSource.RegisterAssertion(new EnumIsNotDefinedAssertCondition<TEnum>(), []);
    }
    
    public static InvokableValueAssertionBuilder<TEnum> HasSameValueAs<TEnum, TExpected>(this IValueSource<TEnum> valueSource, TExpected expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
        where TEnum : Enum
        where TExpected : Enum
    {
        return valueSource.RegisterAssertion(new EnumHasSameValueAsCondition<TEnum, TExpected>(expected),
            [doNotPopulateThisValue1]);
    }
    
    public static InvokableValueAssertionBuilder<TEnum> DoesNotHaveSameValueAs<TEnum, TExpected>(this IValueSource<TEnum> valueSource, TExpected expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
        where TEnum : Enum
        where TExpected : Enum
    {
        return valueSource.RegisterAssertion(new EnumDoesNotHaveSameValueAsCondition<TEnum, TExpected>(expected),
            [doNotPopulateThisValue1]);
    }
}
