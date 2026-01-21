using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Base class for equality assertions that support tolerance-based comparison.
/// Provides common pattern for Double, Float, Long, DateTime, DateTimeOffset, TimeOnly, and DateOnly equality assertions.
/// </summary>
/// <typeparam name="TValue">The type of value being compared</typeparam>
/// <typeparam name="TTolerance">The type used to express tolerance (e.g., double, TimeSpan, int)</typeparam>
public abstract class ToleranceBasedEqualsAssertion<TValue, TTolerance> : Assertion<TValue>
{
    private readonly TValue _expected;
    private TTolerance? _tolerance;

    protected ToleranceBasedEqualsAssertion(
        AssertionContext<TValue> context,
        TValue expected)
        : base(context)
    {
        _expected = expected;
    }

    /// <summary>
    /// Specifies the acceptable tolerance for the comparison.
    /// </summary>
    public ToleranceBasedEqualsAssertion<TValue, TTolerance> Within(TTolerance tolerance)
    {
        _tolerance = tolerance;
        Context.ExpressionBuilder.Append($".Within({tolerance})");
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

        // If tolerance is specified, use tolerance-based comparison
        if (_tolerance != null && HasToleranceValue())
        {
            if (IsWithinTolerance(value!, _expected, _tolerance))
            {
                return AssertionResult._passedTask;
            }

            var difference = CalculateDifference(value!, _expected);
            return Task.FromResult(AssertionResult.Failed(FormatDifferenceMessage(value!, difference)));
        }

        // No tolerance - exact equality check
        if (AreExactlyEqual(value!, _expected))
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    /// <summary>
    /// Checks if tolerance has been set and has a meaningful value (not null/default).
    /// </summary>
    protected abstract bool HasToleranceValue();

    /// <summary>
    /// Determines if the actual value is within the specified tolerance of the expected value.
    /// </summary>
    protected abstract bool IsWithinTolerance(TValue actual, TValue expected, TTolerance tolerance);

    /// <summary>
    /// Calculates the difference between actual and expected values.
    /// Used for error message formatting.
    /// </summary>
    protected abstract object CalculateDifference(TValue actual, TValue expected);

    /// <summary>
    /// Checks if two values are exactly equal (without tolerance).
    /// </summary>
    protected abstract bool AreExactlyEqual(TValue actual, TValue expected);

    /// <summary>
    /// Formats the error message when values are not within tolerance.
    /// Default implementation can be overridden for custom formatting.
    /// </summary>
    protected virtual string FormatDifferenceMessage(TValue actual, object difference)
    {
        return $"found {actual}, which differs by {difference}";
    }

    protected override string GetExpectation()
    {
        if (_tolerance != null && HasToleranceValue())
        {
            return $"to be within {_tolerance} of {_expected}";
        }

        return $"to be {_expected}";
    }
}
