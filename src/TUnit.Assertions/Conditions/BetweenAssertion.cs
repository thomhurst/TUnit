using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is between a minimum and maximum.
/// Demonstrates custom method for inclusive/exclusive bounds.
/// </summary>
[AssertionExtension("IsBetween")]
public class BetweenAssertion<TValue> : Assertion<TValue>
    where TValue : IComparable<TValue>
{
    private readonly TValue _minimum;
    private readonly TValue _maximum;
    private bool _minInclusive = true;
    private bool _maxInclusive = true;

    public BetweenAssertion(
        AssertionContext<TValue> context,
        TValue minimum,
        TValue maximum)
        : base(context)
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
        Context.ExpressionBuilder.Append(".Inclusive()");
        return this;
    }

    /// <summary>
    /// ⚡ Custom method - Makes both bounds exclusive
    /// </summary>
    public BetweenAssertion<TValue> Exclusive()
    {
        _minInclusive = false;
        _maxInclusive = false;
        Context.ExpressionBuilder.Append(".Exclusive()");
        return this;
    }

    /// <summary>
    /// ⚡ Custom method - Minimum inclusive, maximum exclusive [min, max)
    /// </summary>
    public BetweenAssertion<TValue> InclusiveMinimum()
    {
        _minInclusive = true;
        _maxInclusive = false;
        Context.ExpressionBuilder.Append(".InclusiveMinimum()");
        return this;
    }

    /// <summary>
    /// ⚡ Custom method - Minimum exclusive, maximum inclusive (min, max]
    /// </summary>
    public BetweenAssertion<TValue> InclusiveMaximum()
    {
        _minInclusive = false;
        _maxInclusive = true;
        Context.ExpressionBuilder.Append(".InclusiveMaximum()");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("received null"));
        }

        var minComparison = value.CompareTo(_minimum);
        var maxComparison = value.CompareTo(_maximum);

        var minOk = _minInclusive ? minComparison >= 0 : minComparison > 0;
        var maxOk = _maxInclusive ? maxComparison <= 0 : maxComparison < 0;

        if (minOk && maxOk)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"received {value}"));
    }

    protected override string GetExpectation()
    {
        var minBracket = _minInclusive ? "[" : "(";
        var maxBracket = _maxInclusive ? "]" : ")";
        return $"to be between {minBracket}{_minimum}, {_maximum}{maxBracket}";
    }
}
