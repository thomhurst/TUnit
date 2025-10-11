using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Assertion with generic constraint
/// Should generate extension method preserving the IComparable constraint
/// </summary>
[AssertionExtension("IsGreaterThan")]
public class GreaterThanAssertion<TValue> : Assertion<TValue>
    where TValue : IComparable<TValue>
{
    private readonly TValue _expected;

    public GreaterThanAssertion(
        AssertionContext<TValue> context,
        TValue expected)
        : base(context)
    {
        _expected = expected;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        if (value.CompareTo(_expected) > 0)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value {value} was not greater than {_expected}"));
    }

    protected override string GetExpectation() => $"to be greater than {_expected}";
}
