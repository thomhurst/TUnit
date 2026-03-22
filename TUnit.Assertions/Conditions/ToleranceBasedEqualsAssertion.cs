using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Base class for equality assertions that support tolerance-based comparison.
/// </summary>
/// <typeparam name="TValue">The type of value being compared</typeparam>
/// <typeparam name="TTolerance">The type used to express absolute tolerance</typeparam>
public abstract class ToleranceBasedEqualsAssertion<TValue, TTolerance> : Assertion<TValue>
{
    private readonly TValue _expected;
    private TTolerance? _tolerance;
    private double? _relativeTolerancePercent;
    private ToleranceMode _toleranceMode = ToleranceMode.None;

    protected ToleranceBasedEqualsAssertion(
        AssertionContext<TValue> context,
        TValue expected)
        : base(context)
    {
        _expected = expected;
    }

    private enum ToleranceMode
    {
        None,
        Absolute,
        Relative
    }

    /// <summary>
    /// Specifies the acceptable absolute tolerance for the comparison.
    /// </summary>
    public ToleranceBasedEqualsAssertion<TValue, TTolerance> Within(TTolerance tolerance)
    {
        _tolerance = tolerance;
        _relativeTolerancePercent = null;
        _toleranceMode = ToleranceMode.Absolute;

        Context.ExpressionBuilder.Append($".Within({tolerance})");
        return this;
    }

    /// <summary>
    /// Specifies the acceptable relative tolerance percentage for the comparison.
    /// For example, 5 means within 5% of the expected value.
    /// </summary>
    public ToleranceBasedEqualsAssertion<TValue, TTolerance> WithinRelativeTolerance(double percentTolerance)
    {
        if (double.IsNaN(percentTolerance) || double.IsInfinity(percentTolerance) || percentTolerance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(percentTolerance),
                "Relative tolerance must be a finite, non-negative percentage.");
        }

        _relativeTolerancePercent = percentTolerance;
        _tolerance = default;
        _toleranceMode = ToleranceMode.Relative;

        Context.ExpressionBuilder.Append($".WithinRelativeTolerance({percentTolerance})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var actual = value!;

        switch (_toleranceMode)
        {
            case ToleranceMode.Absolute:
                if (_tolerance != null && HasToleranceValue())
                {
                    if (IsWithinTolerance(actual, _expected, _tolerance))
                    {
                        return AssertionResult._passedTask;
                    }

                    var absoluteDifference = CalculateDifference(actual, _expected);
                    return Task.FromResult(
                        AssertionResult.Failed(
                            FormatDifferenceMessage(actual, absoluteDifference)));
                }

                break;

            case ToleranceMode.Relative:
                if (_relativeTolerancePercent.HasValue)
                {
                    if (IsWithinRelativeTolerance(actual, _expected, _relativeTolerancePercent.Value))
                    {
                        return AssertionResult._passedTask;
                    }

                    var relativeDifference = CalculateDifference(actual, _expected);
                    return Task.FromResult(
                        AssertionResult.Failed(
                            FormatRelativeDifferenceMessage(actual, relativeDifference, _relativeTolerancePercent.Value)));
                }

                break;
        }

        return AreExactlyEqual(actual, _expected)
            ? AssertionResult._passedTask
            : Task.FromResult(AssertionResult.Failed($"found {actual}"));
    }

    /// <summary>
    /// Checks if absolute tolerance has been set and has a meaningful value.
    /// </summary>
    protected abstract bool HasToleranceValue();

    /// <summary>
    /// Determines if the actual value is within the specified absolute tolerance of the expected value.
    /// </summary>
    protected abstract bool IsWithinTolerance(TValue actual, TValue expected, TTolerance tolerance);

    /// <summary>
    /// Determines if the actual value is within the specified relative tolerance percentage of the expected value.
    /// Default implementation is unsupported for non-numeric types.
    /// </summary>
    protected virtual bool IsWithinRelativeTolerance(TValue actual, TValue expected, double percentTolerance)
        => throw new NotSupportedException($"{GetType().Name} does not support relative tolerance.");

    /// <summary>
    /// Calculates the difference between actual and expected values.
    /// </summary>
    protected abstract object CalculateDifference(TValue actual, TValue expected);

    /// <summary>
    /// Checks if two values are exactly equal.
    /// </summary>
    protected abstract bool AreExactlyEqual(TValue actual, TValue expected);

    /// <summary>
    /// Formats the failure message for absolute tolerance comparisons.
    /// </summary>
    protected virtual string FormatDifferenceMessage(TValue actual, object difference)
        => $"found {actual}, which differs by {difference}";

    /// <summary>
    /// Formats the failure message for relative tolerance comparisons.
    /// </summary>
    protected virtual string FormatRelativeDifferenceMessage(TValue actual, object difference, double percentTolerance)
        => $"found {actual}, which differs by {difference} and is outside the allowed relative tolerance of {percentTolerance}%";

    protected override string GetExpectation()
    {
        return _toleranceMode switch
        {
            ToleranceMode.Absolute when _tolerance != null && HasToleranceValue()
                => $"to be within {_tolerance} of {_expected}",

            ToleranceMode.Relative when _relativeTolerancePercent.HasValue
                => $"to be within {_relativeTolerancePercent}% of {_expected}",

            _ => $"to be {_expected}"
        };
    }
}
