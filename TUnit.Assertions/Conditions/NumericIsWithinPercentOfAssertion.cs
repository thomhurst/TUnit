using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a double value is within a given percentage of an expected value.
/// |actual - expected| &lt;= |expected * percent / 100|
/// </summary>
[AssertionExtension("IsWithinPercentOf", OverloadResolutionPriority = 2)]
public class DoubleIsWithinPercentOfAssertion : Assertion<double>
{
    private readonly double _expected;
    private readonly double _percent;

    public DoubleIsWithinPercentOfAssertion(
        AssertionContext<double> context,
        double expected,
        double percent)
        : base(context)
    {
        _expected = expected;
        _percent = percent;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<double> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        // Handle NaN comparisons
        if (double.IsNaN(value) && double.IsNaN(_expected))
        {
            return AssertionResult._passedTask;
        }

        if (double.IsNaN(value) || double.IsNaN(_expected))
        {
            return Task.FromResult(AssertionResult.Failed($"found {value}"));
        }

        // Handle infinity
        if (double.IsPositiveInfinity(value) && double.IsPositiveInfinity(_expected))
        {
            return AssertionResult._passedTask;
        }

        if (double.IsNegativeInfinity(value) && double.IsNegativeInfinity(_expected))
        {
            return AssertionResult._passedTask;
        }

        var diff = Math.Abs(value - _expected);
        var allowedDelta = Math.Abs(_expected * _percent / 100.0);

        if (diff <= allowedDelta)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed(
            $"found {value}, which differs by {diff} ({(diff / Math.Abs(_expected)) * 100:F2}% of expected)"));
    }

    protected override string GetExpectation() =>
        $"to be within {_percent}% of {_expected}";
}

/// <summary>
/// Asserts that a float value is within a given percentage of an expected value.
/// |actual - expected| &lt;= |expected * percent / 100|
/// </summary>
[AssertionExtension("IsWithinPercentOf", OverloadResolutionPriority = 2)]
public class FloatIsWithinPercentOfAssertion : Assertion<float>
{
    private readonly float _expected;
    private readonly float _percent;

    public FloatIsWithinPercentOfAssertion(
        AssertionContext<float> context,
        float expected,
        float percent)
        : base(context)
    {
        _expected = expected;
        _percent = percent;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<float> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        // Handle NaN comparisons
        if (float.IsNaN(value) && float.IsNaN(_expected))
        {
            return AssertionResult._passedTask;
        }

        if (float.IsNaN(value) || float.IsNaN(_expected))
        {
            return Task.FromResult(AssertionResult.Failed($"found {value}"));
        }

        // Handle infinity
        if (float.IsPositiveInfinity(value) && float.IsPositiveInfinity(_expected))
        {
            return AssertionResult._passedTask;
        }

        if (float.IsNegativeInfinity(value) && float.IsNegativeInfinity(_expected))
        {
            return AssertionResult._passedTask;
        }

        var diff = Math.Abs(value - _expected);
        var allowedDelta = Math.Abs(_expected * _percent / 100.0f);

        if (diff <= allowedDelta)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed(
            $"found {value}, which differs by {diff} ({(diff / Math.Abs(_expected)) * 100:F2}% of expected)"));
    }

    protected override string GetExpectation() =>
        $"to be within {_percent}% of {_expected}";
}

/// <summary>
/// Asserts that an int value is within a given percentage of an expected value.
/// |actual - expected| &lt;= |expected * percent / 100|
/// </summary>
[AssertionExtension("IsWithinPercentOf", OverloadResolutionPriority = 2)]
public class IntIsWithinPercentOfAssertion : Assertion<int>
{
    private readonly int _expected;
    private readonly double _percent;

    public IntIsWithinPercentOfAssertion(
        AssertionContext<int> context,
        int expected,
        double percent)
        : base(context)
    {
        _expected = expected;
        _percent = percent;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var diff = Math.Abs((double)value - _expected);
        var allowedDelta = Math.Abs(_expected * _percent / 100.0);

        if (diff <= allowedDelta)
        {
            return AssertionResult._passedTask;
        }

        var actualPercent = _expected != 0 ? (diff / Math.Abs(_expected)) * 100 : double.PositiveInfinity;
        return Task.FromResult(AssertionResult.Failed(
            $"found {value}, which differs by {(long)diff} ({actualPercent:F2}% of expected)"));
    }

    protected override string GetExpectation() =>
        $"to be within {_percent}% of {_expected}";
}

/// <summary>
/// Asserts that a long value is within a given percentage of an expected value.
/// |actual - expected| &lt;= |expected * percent / 100|
/// </summary>
[AssertionExtension("IsWithinPercentOf", OverloadResolutionPriority = 2)]
public class LongIsWithinPercentOfAssertion : Assertion<long>
{
    private readonly long _expected;
    private readonly double _percent;

    public LongIsWithinPercentOfAssertion(
        AssertionContext<long> context,
        long expected,
        double percent)
        : base(context)
    {
        _expected = expected;
        _percent = percent;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<long> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var diff = Math.Abs((double)value - _expected);
        var allowedDelta = Math.Abs(_expected * _percent / 100.0);

        if (diff <= allowedDelta)
        {
            return AssertionResult._passedTask;
        }

        var actualPercent = _expected != 0 ? (diff / Math.Abs(_expected)) * 100 : double.PositiveInfinity;
        return Task.FromResult(AssertionResult.Failed(
            $"found {value}, which differs by {(long)diff} ({actualPercent:F2}% of expected)"));
    }

    protected override string GetExpectation() =>
        $"to be within {_percent}% of {_expected}";
}

/// <summary>
/// Asserts that a decimal value is within a given percentage of an expected value.
/// |actual - expected| &lt;= |expected * percent / 100|
/// </summary>
[AssertionExtension("IsWithinPercentOf", OverloadResolutionPriority = 2)]
public class DecimalIsWithinPercentOfAssertion : Assertion<decimal>
{
    private readonly decimal _expected;
    private readonly decimal _percent;

    public DecimalIsWithinPercentOfAssertion(
        AssertionContext<decimal> context,
        decimal expected,
        decimal percent)
        : base(context)
    {
        _expected = expected;
        _percent = percent;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<decimal> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var diff = Math.Abs(value - _expected);
        var allowedDelta = Math.Abs(_expected * _percent / 100m);

        if (diff <= allowedDelta)
        {
            return AssertionResult._passedTask;
        }

        var actualPercent = _expected != 0 ? (diff / Math.Abs(_expected)) * 100m : -1m;
        var percentDisplay = actualPercent >= 0 ? $"{actualPercent:F2}%" : "Infinity%";
        return Task.FromResult(AssertionResult.Failed(
            $"found {value}, which differs by {diff} ({percentDisplay} of expected)"));
    }

    protected override string GetExpectation() =>
        $"to be within {_percent}% of {_expected}";
}
