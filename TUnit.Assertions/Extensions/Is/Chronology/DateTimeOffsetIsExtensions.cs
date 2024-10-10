#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Chronology;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;

namespace TUnit.Assertions.Extensions;

public static class DateTimeOffsetIsExtensions
{
    public static DateTimeOffsetEqualToAssertionBuilderWrapper IsEqualTo(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
    {
        return new DateTimeOffsetEqualToAssertionBuilderWrapper(
            valueSource.RegisterAssertion(new DateTimeOffsetEqualsExpectedValueAssertCondition(expected),
                [doNotPopulateThisValue1])
        );
    }
    
    public static InvokableValueAssertionBuilder<DateTimeOffset> IsAfter(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateTimeOffset> IsAfterOrEqualTo(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateTimeOffset> IsBefore(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }

    public static InvokableValueAssertionBuilder<DateTimeOffset> IsBeforeOrEqualTo(
        this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTimeOffset, DateTimeOffset>(default,
                (value, _, _) => { return value <= expected; },
                (value, _, _) =>
                    $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]);
    }
}