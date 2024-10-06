#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions.Chronology;

public static class TimeSpanIsExtensions
{
    public static InvokableValueAssertionBuilder<TimeSpan> IsEqualTo(this IValueSource<TimeSpan> valueSource, TimeSpan expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TimeSpan, TimeSpan>(expected,
            (actual, _, _) =>
            {
                return actual <= expected.Add(tolerance) && actual >= expected.Subtract(tolerance);
            },
            (timeSpan, _, _) => $"{timeSpan} is not between {timeSpan.Subtract(tolerance)} and {timeSpan.Add(tolerance)}")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]); }
    
    public static InvokableValueAssertionBuilder<TimeSpan> IsZero(this IValueSource<TimeSpan> valueSource)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<TimeSpan>(TimeSpan.Zero)
            , []);
    }
}