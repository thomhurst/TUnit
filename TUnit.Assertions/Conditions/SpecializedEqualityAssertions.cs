using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

#if NET6_0_OR_GREATER
/// <summary>
/// Asserts that a DateOnly value is equal to another, with optional tolerance.
/// </summary>
public class DateOnlyEqualsAssertion : Assertion<DateOnly>
{
    private readonly DateOnly _expected;
    private int? _toleranceDays;

    public DateOnlyEqualsAssertion(
        AssertionContext<DateOnly> context,
        DateOnly expected)
        : base(context)
    {
        _expected = expected;
    }

    public DateOnlyEqualsAssertion WithinDays(int days)
    {
        _toleranceDays = days;
        Context.ExpressionBuilder.Append($".WithinDays({days})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DateOnly> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (_toleranceDays.HasValue)
        {
            var diff = Math.Abs(value.DayNumber - _expected.DayNumber);
            if (diff <= _toleranceDays.Value)
            {
                return Task.FromResult(AssertionResult.Passed);
            }

            return Task.FromResult(AssertionResult.Failed($"found {value}, which is {diff} days from expected"));
        }

        if (value == _expected)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() =>
        _toleranceDays.HasValue
            ? $"to be within {_toleranceDays} days of {_expected}"
            : $"to be {_expected}";
}
#endif

#if NET6_0_OR_GREATER
/// <summary>
/// Asserts that a TimeOnly value is equal to another, with optional tolerance.
/// </summary>
public class TimeOnlyEqualsAssertion : Assertion<TimeOnly>
{
    private readonly TimeOnly _expected;
    private TimeSpan? _tolerance;

    public TimeOnlyEqualsAssertion(
        AssertionContext<TimeOnly> context,
        TimeOnly expected)
        : base(context)
    {
        _expected = expected;
    }

    public TimeOnlyEqualsAssertion Within(TimeSpan tolerance)
    {
        _tolerance = tolerance;
        Context.ExpressionBuilder.Append($".Within({tolerance})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TimeOnly> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (_tolerance.HasValue)
        {
            var diff = value > _expected ? value.ToTimeSpan() - _expected.ToTimeSpan() : _expected.ToTimeSpan() - value.ToTimeSpan();
            if (diff <= _tolerance.Value)
            {
                return Task.FromResult(AssertionResult.Passed);
            }

            return Task.FromResult(AssertionResult.Failed($"found {value}, which is {diff} from expected"));
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
#endif

/// <summary>
/// Asserts that a double value is equal to another, with optional tolerance.
/// </summary>
public class DoubleEqualsAssertion : Assertion<double>
{
    private readonly double _expected;
    private double? _tolerance;

    public DoubleEqualsAssertion(
        AssertionContext<double> context,
        double expected)
        : base(context)
    {
        _expected = expected;
    }

    public DoubleEqualsAssertion Within(double tolerance)
    {
        _tolerance = tolerance;
        Context.ExpressionBuilder.Append($".Within({tolerance})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<double> metadata)
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
public class LongEqualsAssertion : Assertion<long>
{
    private readonly long _expected;
    private long? _tolerance;

    public LongEqualsAssertion(
        AssertionContext<long> context,
        long expected)
        : base(context)
    {
        _expected = expected;
    }

    public LongEqualsAssertion Within(long tolerance)
    {
        _tolerance = tolerance;
        Context.ExpressionBuilder.Append($".Within({tolerance})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<long> metadata)
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
/// Asserts that a DateTimeOffset value is equal to another, with optional tolerance.
/// </summary>
public class DateTimeOffsetEqualsAssertion : Assertion<DateTimeOffset>
{
    private readonly DateTimeOffset _expected;
    private TimeSpan? _tolerance;

    public DateTimeOffsetEqualsAssertion(
        AssertionContext<DateTimeOffset> context,
        DateTimeOffset expected)
        : base(context)
    {
        _expected = expected;
    }

    public DateTimeOffsetEqualsAssertion Within(TimeSpan tolerance)
    {
        _tolerance = tolerance;
        Context.ExpressionBuilder.Append($".Within({tolerance})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DateTimeOffset> metadata)
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
