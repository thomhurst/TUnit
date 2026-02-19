using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is less than a maximum value.
/// </summary>
[AssertionExtension("IsLessThan")]
public class LessThanAssertion<TValue> : Assertion<TValue>
    where TValue : IComparable<TValue>
{
    private readonly TValue _maximum;

    public LessThanAssertion(
        AssertionContext<TValue> context,
        TValue maximum)
        : base(context)
    {
        _maximum = maximum;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("received null"));
        }

        if (value.CompareTo(_maximum) < 0)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"received {value}"));
    }

    protected override string GetExpectation() => $"to be less than {_maximum}";
}

/// <summary>
/// Asserts that a value is less than or equal to a maximum value.
/// </summary>
[AssertionExtension("IsLessThanOrEqualTo")]
public class LessThanOrEqualAssertion<TValue> : Assertion<TValue>
    where TValue : IComparable<TValue>
{
    private readonly TValue _maximum;

    public LessThanOrEqualAssertion(
        AssertionContext<TValue> context,
        TValue maximum)
        : base(context)
    {
        _maximum = maximum;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("received null"));
        }

        if (value.CompareTo(_maximum) <= 0)
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"received {value}"));
    }

    protected override string GetExpectation() => $"to be less than or equal to {_maximum}";
}
