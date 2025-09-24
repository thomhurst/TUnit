using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class TimeSpanEqualToAssertionBuilderWrapper : AssertionBuilderWrapperBase<TimeSpan>
{
    internal TimeSpanEqualToAssertionBuilderWrapper(AssertionBuilder<TimeSpan> invokableAssertionBuilder)
        : base(invokableAssertionBuilder)
    {
    }

    public TimeSpanEqualToAssertionBuilderWrapper Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<TimeSpanEqualsExpectedValueAssertCondition>();
        assertion.SetTolerance(tolerance);
        AppendCallerMethod([doNotPopulateThis]);
        return this;
    }
}
