#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableValueAssertionBuilder<TimeOnly> IsAfter(this IValueSource<TimeOnly> valueSource, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TimeOnly, TimeOnly>(default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TimeOnly> IsAfterOrEqualTo(this IValueSource<TimeOnly> valueSource, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TimeOnly, TimeOnly>(default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TimeOnly> IsBefore(this IValueSource<TimeOnly> valueSource, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TimeOnly, TimeOnly>(default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TimeOnly> IsBeforeOrEqualTo(this IValueSource<TimeOnly> valueSource, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TimeOnly, TimeOnly>(default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TimeOnly> IsBetween(this IValueSource<TimeOnly> valueSource, TimeOnly lowerBound, TimeOnly upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TimeOnly, TimeOnly>(default, (value, _, _, _) => value >= lowerBound && value <= upperBound,
            (value, _, _) => $"{value} was not between {lowerBound} and {upperBound}")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]); }
    
    public static InvokableValueAssertionBuilder<TimeOnly> IsEqualToWithTolerance(this IValueSource<TimeOnly> valueSource, TimeOnly expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
    {
        return IsBetween(valueSource, expected.Add(-tolerance), expected.Add(tolerance), doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}