#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.Extensions;

public static class TimeSpanIsNotExtensions
{
    public static InvokableValueAssertion<TimeSpan> IsNotZero(this IValueSource<TimeSpan> valueSource)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TimeSpan>(TimeSpan.Zero)
            , []);
    }
}
