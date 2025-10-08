using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that two references point to the same object.
/// </summary>
public class SameReferenceAssertion<TValue> : Assertion<TValue>
{
    private readonly object? _expected;

    public SameReferenceAssertion(
        AssertionContext<TValue> context,
        object? expected)
        : base(context)
    {
        _expected = expected;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (ReferenceEquals(value, _expected))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed("references are different"));
    }

    protected override string GetExpectation() => "to be the same reference";
}

/// <summary>
/// Asserts that two references do NOT point to the same object.
/// </summary>
public class NotSameReferenceAssertion<TValue> : Assertion<TValue>
{
    private readonly object? _expected;

    public NotSameReferenceAssertion(
        AssertionContext<TValue> context,
        object? expected)
        : base(context)
    {
        _expected = expected;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (!ReferenceEquals(value, _expected))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed("references are the same"));
    }

    protected override string GetExpectation() => "to not be the same reference";
}
