using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Assertion with multiple parameters
/// Should generate extension method with CallerArgumentExpression for all parameters
/// </summary>
[AssertionExtension("IsBetween")]
public class BetweenAssertion<TValue> : Assertion<TValue>
    where TValue : IComparable<TValue>
{
    private readonly TValue _lowerBound;
    private readonly TValue _upperBound;

    public BetweenAssertion(
        AssertionContext<TValue> context,
        TValue lowerBound,
        TValue upperBound)
        : base(context)
    {
        _lowerBound = lowerBound;
        _upperBound = upperBound;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        if (value.CompareTo(_lowerBound) >= 0 && value.CompareTo(_upperBound) <= 0)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value {value} was not between {_lowerBound} and {_upperBound}"));
    }

    protected override string GetExpectation() => $"to be between {_lowerBound} and {_upperBound}";
}
