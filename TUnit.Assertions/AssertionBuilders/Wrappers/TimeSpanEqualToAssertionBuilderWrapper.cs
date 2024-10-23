using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class TimeSpanEqualToAssertionBuilderWrapper : InvokableValueAssertionBuilder<TimeSpan>
{
    internal TimeSpanEqualToAssertionBuilderWrapper(InvokableAssertionBuilder<TimeSpan> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public TimeSpanEqualToAssertionBuilderWrapper Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        var assertion = (TimeSpanEqualsExpectedValueAssertCondition) Assertions.Peek();

        assertion.SetTolerance(tolerance);

        AppendCallerMethod([doNotPopulateThis]);
        
        return this;
    }
}