using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is between a minimum and maximum.
/// Demonstrates custom method for inclusive/exclusive bounds.
/// </summary>
public class BetweenAssertion<TValue> : Assertion<TValue>
    where TValue : IComparable<TValue>
{
    private readonly TValue _minimum;
    private readonly TValue _maximum;
    private bool _minInclusive = true;
    private bool _maxInclusive = true;

    public BetweenAssertion(
        EvaluationContext<TValue> context,
        TValue minimum,
        TValue maximum,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _minimum = minimum;
        _maximum = maximum;
    }

    /// <summary>
    /// ⚡ Custom method - Makes both bounds inclusive (default)
    /// </summary>
    public BetweenAssertion<TValue> Inclusive()
    {
        _minInclusive = true;
        _maxInclusive = true;
        ExpressionBuilder.Append(".Inclusive()");
        return this;
    }

    /// <summary>
    /// ⚡ Custom method - Makes both bounds exclusive
    /// </summary>
    public BetweenAssertion<TValue> Exclusive()
    {
        _minInclusive = false;
        _maxInclusive = false;
        ExpressionBuilder.Append(".Exclusive()");
        return this;
    }

    /// <summary>
    /// ⚡ Custom method - Minimum inclusive, maximum exclusive [min, max)
    /// </summary>
    public BetweenAssertion<TValue> InclusiveMinimum()
    {
        _minInclusive = true;
        _maxInclusive = false;
        ExpressionBuilder.Append(".InclusiveMinimum()");
        return this;
    }

    /// <summary>
    /// ⚡ Custom method - Minimum exclusive, maximum inclusive (min, max]
    /// </summary>
    public BetweenAssertion<TValue> InclusiveMaximum()
    {
        _minInclusive = false;
        _maxInclusive = true;
        ExpressionBuilder.Append(".InclusiveMaximum()");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value is null"));

        var minComparison = value.CompareTo(_minimum);
        var maxComparison = value.CompareTo(_maximum);

        var minOk = _minInclusive ? minComparison >= 0 : minComparison > 0;
        var maxOk = _maxInclusive ? maxComparison <= 0 : maxComparison < 0;

        if (minOk && maxOk)
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation()
    {
        var minBracket = _minInclusive ? "[" : "(";
        var maxBracket = _maxInclusive ? "]" : ")";
        return $"to be between {minBracket}{_minimum}, {_maximum}{maxBracket}";
    }
}
