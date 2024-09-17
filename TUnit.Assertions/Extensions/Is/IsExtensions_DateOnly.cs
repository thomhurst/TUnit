#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableAssertionBuilder<DateOnly, TAnd, TOr> IsGreaterThan<TAnd, TOr>(this IValueSource<DateOnly, TAnd, TOr> valueSource, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<DateOnly, TAnd, TOr>
        where TOr : IOr<DateOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{value} was not greater than {expected}")
            .ChainedTo(valueSource.AssertionBuilder); }
    
    public static InvokableAssertionBuilder<DateOnly, TAnd, TOr> IsGreaterThanOrEqualTo<TAnd, TOr>(this IValueSource<DateOnly, TAnd, TOr> valueSource, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<DateOnly, TAnd, TOr>
        where TOr : IOr<DateOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{value} was not greater than or equal to {expected}")
            .ChainedTo(valueSource.AssertionBuilder); }
    
    public static InvokableAssertionBuilder<DateOnly, TAnd, TOr> IsLessThan<TAnd, TOr>(this IValueSource<DateOnly, TAnd, TOr> valueSource, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<DateOnly, TAnd, TOr>
        where TOr : IOr<DateOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{value} was not less than {expected}")
            .ChainedTo(valueSource.AssertionBuilder); }
    
    public static InvokableAssertionBuilder<DateOnly, TAnd, TOr> IsLessThanOrEqualTo<TAnd, TOr>(this IValueSource<DateOnly, TAnd, TOr> valueSource, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<DateOnly, TAnd, TOr>
        where TOr : IOr<DateOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _, _) => $"{value} was not less than or equal to {expected}")
            .ChainedTo(valueSource.AssertionBuilder); }
    
    public static InvokableAssertionBuilder<DateOnly, TAnd, TOr> IsBetween<TAnd, TOr>(this IValueSource<DateOnly, TAnd, TOr> valueSource, DateOnly lowerBound, DateOnly upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<DateOnly, TAnd, TOr>
        where TOr : IOr<DateOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _, _) => $"{value} was not between {lowerBound} and {upperBound}")
            .ChainedTo(valueSource.AssertionBuilder); }
    
    public static InvokableAssertionBuilder<DateOnly, TAnd, TOr> IsEqualToWithTolerance<TAnd, TOr>(this IValueSource<DateOnly, TAnd, TOr> valueSource, DateOnly expected, int daysTolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("daysTolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<DateOnly, TAnd, TOr>
        where TOr : IOr<DateOnly, TAnd, TOr>
    {
        return IsBetween(valueSource, expected.AddDays(-daysTolerance), expected.AddDays(daysTolerance), doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}