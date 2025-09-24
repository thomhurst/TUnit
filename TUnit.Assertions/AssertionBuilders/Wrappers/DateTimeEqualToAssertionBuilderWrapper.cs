using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class DateTimeEqualToAssertionBuilderWrapper : AssertionBuilderWrapperBase<DateTime>
{
    internal DateTimeEqualToAssertionBuilderWrapper(AssertionBuilder<DateTime> invokableAssertionBuilder)
        : base(invokableAssertionBuilder)
    {
    }

    public DateTimeEqualToAssertionBuilderWrapper Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<DateTimeEqualsExpectedValueAssertCondition>();
        assertion.SetTolerance(tolerance);
        AppendCallerMethod([doNotPopulateThis]);
        return this;
    }
}
