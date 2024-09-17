#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableAssertionBuilder<TimeOnly, TAnd, TOr> IsAfter<TAnd, TOr>(this IValueSource<TimeOnly, TAnd, TOr> valueSource, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<TimeOnly, TAnd, TOr>
        where TOr : IOr<TimeOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]); }
    
    public static InvokableAssertionBuilder<TimeOnly, TAnd, TOr> IsAfterOrEqualTo<TAnd, TOr>(this IValueSource<TimeOnly, TAnd, TOr> valueSource, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeOnly, TAnd, TOr>
        where TOr : IOr<TimeOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]); }
    
    public static InvokableAssertionBuilder<TimeOnly, TAnd, TOr> IsBefore<TAnd, TOr>(this IValueSource<TimeOnly, TAnd, TOr> valueSource, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeOnly, TAnd, TOr>
        where TOr : IOr<TimeOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]); }
    
    public static InvokableAssertionBuilder<TimeOnly, TAnd, TOr> IsBeforeOrEqualTo<TAnd, TOr>(this IValueSource<TimeOnly, TAnd, TOr> valueSource, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeOnly, TAnd, TOr>
        where TOr : IOr<TimeOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]); }
    
    public static InvokableAssertionBuilder<TimeOnly, TAnd, TOr> IsBetween<TAnd, TOr>(this IValueSource<TimeOnly, TAnd, TOr> valueSource, TimeOnly lowerBound, TimeOnly upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<TimeOnly, TAnd, TOr>
        where TOr : IOr<TimeOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(default, (value, _, _, _) => value >= lowerBound && value <= upperBound,
            (value, _, _) => $"{value} was not between {lowerBound} and {upperBound}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]); }
    
    public static InvokableAssertionBuilder<TimeOnly, TAnd, TOr> IsEqualToWithTolerance<TAnd, TOr>(this IValueSource<TimeOnly, TAnd, TOr> valueSource, TimeOnly expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<TimeOnly, TAnd, TOr>
        where TOr : IOr<TimeOnly, TAnd, TOr>
    {
        return IsBetween(valueSource, expected.Add(-tolerance), expected.Add(tolerance), doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}