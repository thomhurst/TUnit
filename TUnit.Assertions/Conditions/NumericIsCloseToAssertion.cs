using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a double value is close to an expected value within an absolute tolerance.
/// |actual - expected| &lt;= tolerance
/// </summary>
[AssertionExtension("IsCloseTo", OverloadResolutionPriority = 2)]
public class DoubleIsCloseToAssertion : Assertion<double>
{
    private readonly double _expected;
    private readonly double _tolerance;

    public DoubleIsCloseToAssertion(
        AssertionContext<double> context,
        double expected,
        double tolerance)
        : base(context)
    {
        _expected = expected;
        _tolerance = tolerance;
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

        if (diff <= _tolerance)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}, which differs by {diff}"));
    }

    protected override string GetExpectation() =>
        $"to be close to {_expected} within tolerance {_tolerance}";
}

/// <summary>
/// Asserts that a float value is close to an expected value within an absolute tolerance.
/// |actual - expected| &lt;= tolerance
/// </summary>
[AssertionExtension("IsCloseTo", OverloadResolutionPriority = 2)]
public class FloatIsCloseToAssertion : Assertion<float>
{
    private readonly float _expected;
    private readonly float _tolerance;

    public FloatIsCloseToAssertion(
        AssertionContext<float> context,
        float expected,
        float tolerance)
        : base(context)
    {
        _expected = expected;
        _tolerance = tolerance;
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

        if (diff <= _tolerance)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}, which differs by {diff}"));
    }

    protected override string GetExpectation() =>
        $"to be close to {_expected} within tolerance {_tolerance}";
}

/// <summary>
/// Asserts that an int value is close to an expected value within an absolute tolerance.
/// |actual - expected| &lt;= tolerance
/// </summary>
[AssertionExtension("IsCloseTo", OverloadResolutionPriority = 2)]
public class IntIsCloseToAssertion : Assertion<int>
{
    private readonly int _expected;
    private readonly int _tolerance;

    public IntIsCloseToAssertion(
        AssertionContext<int> context,
        int expected,
        int tolerance)
        : base(context)
    {
        _expected = expected;
        _tolerance = tolerance;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var diff = Math.Abs((long)value - _expected);

        if (diff <= _tolerance)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}, which differs by {diff}"));
    }

    protected override string GetExpectation() =>
        $"to be close to {_expected} within tolerance {_tolerance}";
}

/// <summary>
/// Asserts that a long value is close to an expected value within an absolute tolerance.
/// |actual - expected| &lt;= tolerance
/// </summary>
[AssertionExtension("IsCloseTo", OverloadResolutionPriority = 2)]
public class LongIsCloseToAssertion : Assertion<long>
{
    private readonly long _expected;
    private readonly long _tolerance;

    public LongIsCloseToAssertion(
        AssertionContext<long> context,
        long expected,
        long tolerance)
        : base(context)
    {
        _expected = expected;
        _tolerance = tolerance;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<long> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var diff = Math.Abs(value - _expected);

        if (diff <= _tolerance)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}, which differs by {diff}"));
    }

    protected override string GetExpectation() =>
        $"to be close to {_expected} within tolerance {_tolerance}";
}

/// <summary>
/// Asserts that a decimal value is close to an expected value within an absolute tolerance.
/// |actual - expected| &lt;= tolerance
/// </summary>
[AssertionExtension("IsCloseTo", OverloadResolutionPriority = 2)]
public class DecimalIsCloseToAssertion : Assertion<decimal>
{
    private readonly decimal _expected;
    private readonly decimal _tolerance;

    public DecimalIsCloseToAssertion(
        AssertionContext<decimal> context,
        decimal expected,
        decimal tolerance)
        : base(context)
    {
        _expected = expected;
        _tolerance = tolerance;
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

        if (diff <= _tolerance)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}, which differs by {diff}"));
    }

    protected override string GetExpectation() =>
        $"to be close to {_expected} within tolerance {_tolerance}";
}
