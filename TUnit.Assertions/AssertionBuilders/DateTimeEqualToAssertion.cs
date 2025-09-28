using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Chronology;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Assertions.Base;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.AssertionBuilders.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Clean DateTime equality assertion - no inheritance, just configuration
/// </summary>
public class DateTimeEqualToAssertion : Assertion<DateTime>
{
    private readonly DateTime _expected;
    private readonly string?[] _expressions;
    
    // Configuration
    private TimeSpan? _tolerance;

    internal DateTimeEqualToAssertion(IValueSource<DateTime> source, DateTime expected, string?[] expressions, IAssertionChain chain = null!)
        : base(source, chain)
    {
        _expected = expected;
        _expressions = expressions;
    }

    public DateTimeEqualToAssertion Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        _tolerance = tolerance;
        return this;
    }

    protected override BaseAssertCondition? CreateCondition()
    {
        var condition = new DateTimeEqualsExpectedValueAssertCondition(_expected);
        if (_tolerance.HasValue)
            condition.SetTolerance(_tolerance.Value);
        return condition;
    }
}
