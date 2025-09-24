using System;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for DateOnly equality comparisons
/// </summary>
#if NET6_0_OR_GREATER
public class DateOnlyEqualToAssertion : FluentAssertionBase<DateOnly, DateOnlyEqualToAssertion>
{
    internal DateOnlyEqualToAssertion(AssertionBuilder<DateOnly> assertionBuilder)
        : base(assertionBuilder)
    {
    }

    public DateOnlyEqualToAssertion WithinDays(int days, [CallerArgumentExpression(nameof(days))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<IDateToleranceCondition>();
        assertion?.SetTolerance(days);
        
        AppendCallerMethod([doNotPopulateThis]);
        return this;
    }
}
#endif