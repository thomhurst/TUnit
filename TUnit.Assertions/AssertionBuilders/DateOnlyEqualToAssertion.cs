using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Chronology;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Assertions.Base;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Clean DateOnly equality assertion - no inheritance, just configuration
/// </summary>
#if NET6_0_OR_GREATER
public class DateOnlyEqualToAssertion : Assertion<DateOnly>
{
    private readonly DateOnly _expected;
    private readonly string?[] _expressions;
    
    // Configuration
    private int? _toleranceDays;

    internal DateOnlyEqualToAssertion(IValueSource<DateOnly> source, DateOnly expected, string?[] expressions, IAssertionChain chain = null!)
        : base(source, chain)
    {
        _expected = expected;
        _expressions = expressions;
    }

    public DateOnlyEqualToAssertion WithinDays(int days, [CallerArgumentExpression(nameof(days))] string doNotPopulateThis = "")
    {
        _toleranceDays = days;
        return this;
    }

    protected override BaseAssertCondition? CreateCondition()
    {
        var condition = new DateOnlyEqualsExpectedValueAssertCondition(_expected);
        if (_toleranceDays.HasValue)
            condition.SetTolerance(_toleranceDays.Value);
        return condition;
    }
}
#endif