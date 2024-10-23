using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class TimeOnlyEqualToAssertionBuilderWrapper : InvokableValueAssertionBuilder<TimeOnly>
{
    internal TimeOnlyEqualToAssertionBuilderWrapper(InvokableAssertionBuilder<TimeOnly> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public TimeOnlyEqualToAssertionBuilderWrapper Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        var assertion = (TimeOnlyEqualsExpectedValueAssertCondition) Assertions.Peek();

        assertion.SetTolerance(tolerance);
        
        AppendCallerMethod([doNotPopulateThis]);
        
        return this;
    }
}