#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Chronology;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;

namespace TUnit.Assertions.Extensions;

public static class DateTimeIsExtensions
{
    public static DateTimeEqualToAssertionBuilderWrapper IsEqualTo(this IValueSource<DateTime> valueSource, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
    {
        return new DateTimeEqualToAssertionBuilderWrapper(
            valueSource.RegisterAssertion(new DateTimeEqualsExpectedValueAssertCondition(expected),
                [doNotPopulateThisValue1])
        );
    }
    
    public static InvokableValueAssertionBuilder<DateTime> IsAfter(this IValueSource<DateTime> valueSource, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTime, DateTime>(default, (value, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateTime> IsAfterOrEqualTo(this IValueSource<DateTime> valueSource, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTime, DateTime>(default, (value, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateTime> IsBefore(this IValueSource<DateTime> valueSource, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTime, DateTime>(default, (value, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }

    public static InvokableValueAssertionBuilder<DateTime> IsBeforeOrEqualTo(this IValueSource<DateTime> valueSource,
        DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<DateTime, DateTime>(default,
                (value, _, _) => { return value <= expected; },
                (value, _, _) =>
                    $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]);
    }
}