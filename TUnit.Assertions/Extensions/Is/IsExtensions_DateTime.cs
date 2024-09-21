#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableValueAssertionBuilder<DateTime> IsBetween(this IValueSource<DateTime> valueSource, DateTime lowerBound, DateTime upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateTime, DateTime>(default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not between {lowerBound.ToLongStringWithMilliseconds()} and {upperBound.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]); }
    
    public static InvokableValueAssertionBuilder<DateTime> IsAfter(this IValueSource<DateTime> valueSource, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateTime, DateTime>(default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateTime> IsAfterOrEqualTo(this IValueSource<DateTime> valueSource, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateTime, DateTime>(default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateTime> IsBefore(this IValueSource<DateTime> valueSource, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateTime, DateTime>(default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateTime> IsBeforeOrEqualTo(this IValueSource<DateTime> valueSource, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<DateTime, DateTime>(default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<DateTime> IsEqualToWithTolerance(this IValueSource<DateTime> valueSource, DateTime expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
    {
        return IsBetween(valueSource, expected - tolerance, expected + tolerance, doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}