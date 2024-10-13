#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class TimeSpanIsNotExtensions
{
    public static InvokableValueAssertionBuilder<TimeSpan> IsNotZero(this IValueSource<TimeSpan> valueSource)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TimeSpan>(TimeSpan.Zero)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TimeSpan> IsNotGreaterThan(this IValueSource<TimeSpan> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TimeSpan, TimeSpan>(default, (value, _, _) => value <= expected,
            (value, _, _) => $"{value} was greater than {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TimeSpan> IsNotGreaterThanOrEqualTo(this IValueSource<TimeSpan> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TimeSpan, TimeSpan>(default, (value, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{value} was greater than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TimeSpan> IsNotLessThan(this IValueSource<TimeSpan> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TimeSpan, TimeSpan>(default, (value, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{value} was less than {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TimeSpan> IsNotLessThanOrEqualTo(this IValueSource<TimeSpan> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TimeSpan, TimeSpan>(default, (value, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{value} was less than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
}