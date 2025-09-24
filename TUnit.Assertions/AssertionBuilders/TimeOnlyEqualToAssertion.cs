using System;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for TimeOnly equality comparisons
/// </summary>
#if NET6_0_OR_GREATER
public class TimeOnlyEqualToAssertion : FluentAssertionBase<TimeOnly, TimeOnlyEqualToAssertion>
{
    internal TimeOnlyEqualToAssertion(AssertionBuilder<TimeOnly> assertionBuilder)
        : base(assertionBuilder)
    {
    }

    public TimeOnlyEqualToAssertion Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<ITimeToleranceCondition>();
        assertion?.SetTolerance(tolerance);
        
        AppendCallerMethod([doNotPopulateThis]);
        return this;
    }
}
#endif