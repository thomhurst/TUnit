using System;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for TimeSpan equality comparisons
/// </summary>
public class TimeSpanEqualToAssertion : FluentAssertionBase<TimeSpan, TimeSpanEqualToAssertion>
{
    internal TimeSpanEqualToAssertion(AssertionBuilder<TimeSpan> assertionBuilder)
        : base(assertionBuilder)
    {
    }

    public TimeSpanEqualToAssertion Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<TimeSpanEqualsExpectedValueAssertCondition>();
        assertion?.SetTolerance(tolerance);
        AppendCallerMethod([doNotPopulateThis]);
        return Self;
    }
}