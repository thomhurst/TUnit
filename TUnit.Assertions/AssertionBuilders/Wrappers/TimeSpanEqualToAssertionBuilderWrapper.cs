using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class TimeSpanEqualToAssertionBuilderWrapper : AssertionBuilder<TimeSpan>
{
    internal TimeSpanEqualToAssertionBuilderWrapper(AssertionBuilder<TimeSpan> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public TimeSpanEqualToAssertionBuilderWrapper Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        var assertion = (TimeSpanEqualsExpectedValueAssertCondition) base.Assertions.Peek();

        assertion.SetTolerance(tolerance);

        AppendCallerMethod([doNotPopulateThis]);

        return this;
    }
}
