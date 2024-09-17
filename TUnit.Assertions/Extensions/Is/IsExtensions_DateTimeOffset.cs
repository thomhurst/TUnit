#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableAssertionBuilder<DateTimeOffset, TAnd, TOr> IsBetween<TAnd, TOr>(this IValueSource<DateTimeOffset, TAnd, TOr> valueSource, DateTimeOffset lowerBound, DateTimeOffset upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<DateTimeOffset, TAnd, TOr>
        where TOr : IOr<DateTimeOffset, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not between {lowerBound.ToLongStringWithMilliseconds()} and {upperBound.ToLongStringWithMilliseconds()}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]); }
    
    public static InvokableAssertionBuilder<DateTimeOffset, TAnd, TOr> IsAfter<TAnd, TOr>(this IValueSource<DateTimeOffset, TAnd, TOr> valueSource, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<DateTimeOffset, TAnd, TOr>
        where TOr : IOr<DateTimeOffset, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]); }
    
    public static InvokableAssertionBuilder<DateTimeOffset, TAnd, TOr> IsAfterOrEqualTo<TAnd, TOr>(this IValueSource<DateTimeOffset, TAnd, TOr> valueSource, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<DateTimeOffset, TAnd, TOr>
        where TOr : IOr<DateTimeOffset, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]); }
    
    public static InvokableAssertionBuilder<DateTimeOffset, TAnd, TOr> IsBefore<TAnd, TOr>(this IValueSource<DateTimeOffset, TAnd, TOr> valueSource, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<DateTimeOffset, TAnd, TOr>
        where TOr : IOr<DateTimeOffset, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]); }
    
    public static InvokableAssertionBuilder<DateTimeOffset, TAnd, TOr> IsBeforeOrEqualTo<TAnd, TOr>(this IValueSource<DateTimeOffset, TAnd, TOr> valueSource, DateTimeOffset expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<DateTimeOffset, TAnd, TOr>
        where TOr : IOr<DateTimeOffset, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateTimeOffset, DateTimeOffset>(default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]); }
    
    
    public static InvokableAssertionBuilder<DateTimeOffset, TAnd, TOr> IsEqualToWithTolerance<TAnd, TOr>(this IValueSource<DateTimeOffset, TAnd, TOr> valueSource, DateTimeOffset expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<DateTimeOffset, TAnd, TOr>
        where TOr : IOr<DateTimeOffset, TAnd, TOr>
    {
        return IsBetween(valueSource, expected - tolerance, expected + tolerance, doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}