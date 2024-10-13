﻿#nullable disable

using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

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