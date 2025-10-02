using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class DateTimeEqualToAssertionBuilderWrapper : InvokableValueAssertion<DateTime>
{
    internal DateTimeEqualToAssertionBuilderWrapper(InvokableAssertion<DateTime> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public DateTimeEqualToAssertionBuilderWrapper Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        var assertion = (DateTimeEqualsExpectedValueAssertCondition) Assertions.Peek();

        assertion.SetTolerance(tolerance);

        AppendCallerMethod([doNotPopulateThis]);

        return this;
    }
}
