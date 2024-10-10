#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;

namespace TUnit.Assertions.Extensions;

public static class TimeSpanIsExtensions
{
    public static TimeSpanEqualToAssertionBuilderWrapper IsEqualTo(this IValueSource<TimeSpan> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
    {
        return new TimeSpanEqualToAssertionBuilderWrapper(
            valueSource.RegisterAssertion(new TimeSpanEqualsExpectedValueAssertCondition(expected),
                [doNotPopulateThisValue1])
        );
    }
    
    public static InvokableValueAssertionBuilder<TimeSpan> IsZero(this IValueSource<TimeSpan> valueSource)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<TimeSpan>(TimeSpan.Zero)
            , []);
    }
}