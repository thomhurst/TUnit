using System;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for DateTimeOffset equality comparisons
/// </summary>
public class DateTimeOffsetEqualToAssertion : FluentAssertionBase<DateTimeOffset, DateTimeOffsetEqualToAssertion>
{
    internal DateTimeOffsetEqualToAssertion(AssertionBuilder<DateTimeOffset> assertionBuilder)
        : base(assertionBuilder)
    {
    }

    public DateTimeOffsetEqualToAssertion Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<DateTimeOffsetEqualsExpectedValueAssertCondition>();
        assertion?.SetTolerance(tolerance);
        AppendCallerMethod([doNotPopulateThis]);
        return Self;
    }
}