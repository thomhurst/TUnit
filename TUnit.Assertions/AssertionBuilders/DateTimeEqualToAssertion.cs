using System;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for DateTime equality comparisons
/// </summary>
public class DateTimeEqualToAssertion : FluentAssertionBase<DateTime, DateTimeEqualToAssertion>
{
    internal DateTimeEqualToAssertion(AssertionBuilder<DateTime> assertionBuilder)
        : base(assertionBuilder)
    {
    }

    public DateTimeEqualToAssertion Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<DateTimeEqualsExpectedValueAssertCondition>();
        assertion?.SetTolerance(tolerance);
        AppendCallerMethod([doNotPopulateThis]);
        return Self;
    }
}