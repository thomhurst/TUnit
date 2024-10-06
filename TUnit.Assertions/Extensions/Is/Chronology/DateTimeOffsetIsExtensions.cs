#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions.Chronology;

public static class DateTimeOffsetIsExtensions
{
    public static InvokableValueAssertionBuilder<DateTimeOffset> IsBetween(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset lowerBound, DateTimeOffset upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not between {lowerBound.ToLongStringWithMilliseconds()} and {upperBound.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]); }
    
    public static InvokableValueAssertionBuilder<DateTimeOffset> IsAfter(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateTimeOffset> IsAfterOrEqualTo(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateTimeOffset> IsBefore(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateTimeOffset> IsBeforeOrEqualTo(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    
    public static InvokableValueAssertionBuilder<DateTimeOffset> IsEqualToWithTolerance(this IValueSource<DateTimeOffset> valueSource, DateTimeOffset expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
    {
        return IsBetween(valueSource, expected - tolerance, expected + tolerance, doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}