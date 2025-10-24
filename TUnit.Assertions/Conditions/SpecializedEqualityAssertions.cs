using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

#if NET6_0_OR_GREATER
/// <summary>
/// Asserts that a DateOnly value is equal to another, with optional tolerance.
/// </summary>
[AssertionExtension("IsEqualTo", OverloadResolutionPriority = 2)]
public class DateOnlyEqualsAssertion : ToleranceBasedEqualsAssertion<DateOnly, int>
{
    public DateOnlyEqualsAssertion(
        AssertionContext<DateOnly> context,
        DateOnly expected)
        : base(context, expected)
    {
    }

    public DateOnlyEqualsAssertion WithinDays(int days)
    {
        Within(days);
        return this;
    }

    protected override bool HasToleranceValue()
    {
        return true; // int? always has meaningful value when not null
    }

    protected override bool IsWithinTolerance(DateOnly actual, DateOnly expected, int toleranceDays)
    {
        var diff = Math.Abs(actual.DayNumber - expected.DayNumber);
        return diff <= toleranceDays;
    }

    protected override object CalculateDifference(DateOnly actual, DateOnly expected)
    {
        return Math.Abs(actual.DayNumber - expected.DayNumber);
    }

    protected override bool AreExactlyEqual(DateOnly actual, DateOnly expected)
    {
        return actual == expected;
    }

    protected override string FormatDifferenceMessage(DateOnly actual, object difference)
    {
        return $"found {actual}, which is {difference} days from expected";
    }

    protected override string GetExpectation()
    {
        // Override to use "days" instead of just the tolerance value
        var baseExpectation = base.GetExpectation();
        return baseExpectation.Replace("to be within ", "to be within ").Replace(" of ", " days of ");
    }
}
#endif

#if NET6_0_OR_GREATER
/// <summary>
/// Asserts that a TimeOnly value is equal to another, with optional tolerance.
/// </summary>
[AssertionExtension("IsEqualTo", OverloadResolutionPriority = 2)]
public class TimeOnlyEqualsAssertion : ToleranceBasedEqualsAssertion<TimeOnly, TimeSpan>
{
    public TimeOnlyEqualsAssertion(
        AssertionContext<TimeOnly> context,
        TimeOnly expected)
        : base(context, expected)
    {
    }

    protected override bool HasToleranceValue()
    {
        return true; // TimeSpan? always has meaningful value when not null
    }

    protected override bool IsWithinTolerance(TimeOnly actual, TimeOnly expected, TimeSpan tolerance)
    {
        var diff = actual > expected ? actual.ToTimeSpan() - expected.ToTimeSpan() : expected.ToTimeSpan() - actual.ToTimeSpan();
        return diff <= tolerance;
    }

    protected override object CalculateDifference(TimeOnly actual, TimeOnly expected)
    {
        return actual > expected ? actual.ToTimeSpan() - expected.ToTimeSpan() : expected.ToTimeSpan() - actual.ToTimeSpan();
    }

    protected override bool AreExactlyEqual(TimeOnly actual, TimeOnly expected)
    {
        return actual == expected;
    }

    protected override string FormatDifferenceMessage(TimeOnly actual, object difference)
    {
        return $"found {actual}, which is {difference} from expected";
    }
}
#endif

/// <summary>
/// Asserts that a double value is equal to another, with optional tolerance.
/// </summary>
[AssertionExtension("IsEqualTo", OverloadResolutionPriority = 2)]
public class DoubleEqualsAssertion : ToleranceBasedEqualsAssertion<double, double>
{
    public DoubleEqualsAssertion(
        AssertionContext<double> context,
        double expected)
        : base(context, expected)
    {
    }

    protected override bool HasToleranceValue()
    {
        return true; // double? always has meaningful value when not null
    }

    protected override bool IsWithinTolerance(double actual, double expected, double tolerance)
    {
        // Handle NaN comparisons: NaN is only equal to NaN
        if (double.IsNaN(actual) && double.IsNaN(expected))
        {
            return true;
        }

        if (double.IsNaN(actual) || double.IsNaN(expected))
        {
            return false;
        }

        // Handle infinity: infinity equals infinity
        if (double.IsPositiveInfinity(actual) && double.IsPositiveInfinity(expected))
        {
            return true;
        }

        if (double.IsNegativeInfinity(actual) && double.IsNegativeInfinity(expected))
        {
            return true;
        }

        var diff = Math.Abs(actual - expected);
        return diff <= tolerance;
    }

    protected override object CalculateDifference(double actual, double expected)
    {
        return Math.Abs(actual - expected);
    }

    protected override bool AreExactlyEqual(double actual, double expected)
    {
        return double.Equals(actual, expected);
    }
}

/// <summary>
/// Asserts that a float value is equal to another, with optional tolerance.
/// </summary>
[AssertionExtension("IsEqualTo", OverloadResolutionPriority = 2)]
public class FloatEqualsAssertion : ToleranceBasedEqualsAssertion<float, float>
{
    public FloatEqualsAssertion(
        AssertionContext<float> context,
        float expected)
        : base(context, expected)
    {
    }

    protected override bool HasToleranceValue()
    {
        return true; // float? always has meaningful value when not null
    }

    protected override bool IsWithinTolerance(float actual, float expected, float tolerance)
    {
        // Handle NaN comparisons: NaN is only equal to NaN
        if (float.IsNaN(actual) && float.IsNaN(expected))
        {
            return true;
        }

        if (float.IsNaN(actual) || float.IsNaN(expected))
        {
            return false;
        }

        // Handle infinity: infinity equals infinity
        if (float.IsPositiveInfinity(actual) && float.IsPositiveInfinity(expected))
        {
            return true;
        }

        if (float.IsNegativeInfinity(actual) && float.IsNegativeInfinity(expected))
        {
            return true;
        }

        var diff = Math.Abs(actual - expected);
        return diff <= tolerance;
    }

    protected override object CalculateDifference(float actual, float expected)
    {
        return Math.Abs(actual - expected);
    }

    protected override bool AreExactlyEqual(float actual, float expected)
    {
        return float.Equals(actual, expected);
    }
}

/// <summary>
/// Asserts that an int value is equal to another, with optional tolerance.
/// </summary>
[AssertionExtension("IsEqualTo", OverloadResolutionPriority = 2)]
public class IntEqualsAssertion : Assertion<int>
{
    private readonly int _expected;
    private int? _tolerance;

    public IntEqualsAssertion(
        AssertionContext<int> context,
        int expected)
        : base(context)
    {
        _expected = expected;
    }

    public IntEqualsAssertion Within(int tolerance)
    {
        _tolerance = tolerance;
        Context.ExpressionBuilder.Append($".Within({tolerance})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (_tolerance.HasValue)
        {
            var diff = Math.Abs(value - _expected);
            if (diff <= _tolerance.Value)
            {
                return Task.FromResult(AssertionResult.Passed);
            }

            return Task.FromResult(AssertionResult.Failed($"found {value}, which differs by {diff}"));
        }

        if (value == _expected)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() =>
        _tolerance.HasValue
            ? $"to be within {_tolerance} of {_expected}"
            : $"to be {_expected}";
}

/// <summary>
/// Asserts that a long value is equal to another, with optional tolerance.
/// </summary>
[AssertionExtension("IsEqualTo", OverloadResolutionPriority = 2)]
public class LongEqualsAssertion : ToleranceBasedEqualsAssertion<long, long>
{
    public LongEqualsAssertion(
        AssertionContext<long> context,
        long expected)
        : base(context, expected)
    {
    }

    protected override bool HasToleranceValue()
    {
        return true; // long? always has meaningful value when not null
    }

    protected override bool IsWithinTolerance(long actual, long expected, long tolerance)
    {
        var diff = Math.Abs(actual - expected);
        return diff <= tolerance;
    }

    protected override object CalculateDifference(long actual, long expected)
    {
        return Math.Abs(actual - expected);
    }

    protected override bool AreExactlyEqual(long actual, long expected)
    {
        return actual == expected;
    }
}

/// <summary>
/// Asserts that a decimal value is equal to another, with optional tolerance.
/// </summary>
[AssertionExtension("IsEqualTo", OverloadResolutionPriority = 2)]
public class DecimalEqualsAssertion : ToleranceBasedEqualsAssertion<decimal, decimal>
{
    public DecimalEqualsAssertion(
        AssertionContext<decimal> context,
        decimal expected)
        : base(context, expected)
    {
    }

    protected override bool HasToleranceValue()
    {
        return true; // decimal? always has meaningful value when not null
    }

    protected override bool IsWithinTolerance(decimal actual, decimal expected, decimal tolerance)
    {
        var diff = Math.Abs(actual - expected);
        return diff <= tolerance;
    }

    protected override object CalculateDifference(decimal actual, decimal expected)
    {
        return Math.Abs(actual - expected);
    }

    protected override bool AreExactlyEqual(decimal actual, decimal expected)
    {
        return actual == expected;
    }
}

/// <summary>
/// Asserts that a DateTimeOffset value is equal to another, with optional tolerance.
/// </summary>
[AssertionExtension("IsEqualTo", OverloadResolutionPriority = 2)]
public class DateTimeOffsetEqualsAssertion : ToleranceBasedEqualsAssertion<DateTimeOffset, TimeSpan>
{
    public DateTimeOffsetEqualsAssertion(
        AssertionContext<DateTimeOffset> context,
        DateTimeOffset expected)
        : base(context, expected)
    {
    }

    protected override bool HasToleranceValue()
    {
        return true; // TimeSpan? always has meaningful value when not null
    }

    protected override bool IsWithinTolerance(DateTimeOffset actual, DateTimeOffset expected, TimeSpan tolerance)
    {
        var diff = actual > expected ? actual - expected : expected - actual;
        return diff <= tolerance;
    }

    protected override object CalculateDifference(DateTimeOffset actual, DateTimeOffset expected)
    {
        return actual > expected ? actual - expected : expected - actual;
    }

    protected override bool AreExactlyEqual(DateTimeOffset actual, DateTimeOffset expected)
    {
        return actual == expected;
    }
}

/// <summary>
/// Asserts that a TimeSpan value is equal to another, with optional tolerance.
/// </summary>
[AssertionExtension("IsEqualTo", OverloadResolutionPriority = 2)]
public class TimeSpanEqualsAssertion : Assertion<TimeSpan>
{
    private readonly TimeSpan _expected;
    private TimeSpan? _tolerance;

    public TimeSpanEqualsAssertion(
        AssertionContext<TimeSpan> context,
        TimeSpan expected)
        : base(context)
    {
        _expected = expected;
    }

    public TimeSpanEqualsAssertion Within(TimeSpan tolerance)
    {
        _tolerance = tolerance;
        Context.ExpressionBuilder.Append($".Within({tolerance})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TimeSpan> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (_tolerance.HasValue)
        {
            var diff = value > _expected ? value - _expected : _expected - value;
            if (diff <= _tolerance.Value)
            {
                return Task.FromResult(AssertionResult.Passed);
            }

            return Task.FromResult(AssertionResult.Failed($"found {value}, which differs by {diff}"));
        }

        if (value == _expected)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() =>
        _tolerance.HasValue
            ? $"to be within {_tolerance} of {_expected}"
            : $"to be {_expected}";
}
