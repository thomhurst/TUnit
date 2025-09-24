#if NET

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class TimeOnlyEqualToAssertionBuilderWrapper : AssertionBuilderWrapperBase<TimeOnly>
{
    internal TimeOnlyEqualToAssertionBuilderWrapper(AssertionBuilder<TimeOnly> invokableAssertionBuilder)
        : base(invokableAssertionBuilder)
    {
    }

    public TimeOnlyEqualToAssertionBuilderWrapper Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<TimeOnlyEqualsExpectedValueAssertCondition>();

        assertion.SetTolerance(tolerance);
        
        AppendCallerMethod([doNotPopulateThis]);
        
        return this;
    }
}

#endif
