using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Assertion with optional parameter
/// Should generate extension method with default parameter value
/// </summary>
[AssertionExtension("IsNotEqualTo")]
public class NotEqualsAssertion<TValue> : Assertion<TValue>
{
    private readonly TValue _notExpected;
    private readonly IEqualityComparer<TValue>? _comparer;

    public NotEqualsAssertion(
        AssertionContext<TValue> context,
        TValue notExpected,
        IEqualityComparer<TValue>? comparer = null)
        : base(context)
    {
        _notExpected = notExpected;
        _comparer = comparer;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var comparer = _comparer ?? EqualityComparer<TValue>.Default;

        if (!comparer.Equals(value!, _notExpected))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"both values are {value}"));
    }

    protected override string GetExpectation() => $"to not be equal to {_notExpected}";
}
