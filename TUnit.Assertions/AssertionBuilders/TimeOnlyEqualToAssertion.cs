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
/// Clean TimeOnly equality assertion - no inheritance, just configuration
/// </summary>
#if NET6_0_OR_GREATER
public class TimeOnlyEqualToAssertion : Assertion<TimeOnly>
{
    private readonly TimeOnly _expected;
    private readonly string?[] _expressions;
    
    // Configuration
    private TimeSpan? _tolerance;

    internal TimeOnlyEqualToAssertion(IValueSource<TimeOnly> source, TimeOnly expected, string?[] expressions, IAssertionChain chain = null!)
        : base(source, chain)
    {
        _expected = expected;
        _expressions = expressions;
    }

    public TimeOnlyEqualToAssertion Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        _tolerance = tolerance;
        return this;
    }

    protected override BaseAssertCondition? CreateCondition()
    {
        var condition = new TimeOnlyEqualsExpectedValueAssertCondition(_expected);
        if (_tolerance.HasValue)
            condition.SetTolerance(_tolerance.Value);
        return condition;
    }
}
#endif