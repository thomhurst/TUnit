#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Assertions.Chronology;

public static class TimeSpanIsNotExtensions
{
    public static InvokableValueAssertionBuilder<TimeSpan> IsNotZero(this IValueSource<TimeSpan> valueSource)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TimeSpan>(TimeSpan.Zero)
            , []);
    }
}