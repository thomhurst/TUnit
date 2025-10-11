using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Assertion with multiple constructors
/// Should generate multiple extension method overloads
/// </summary>
[AssertionExtension("IsEqualTo")]
public class EqualsAssertion<TValue> : Assertion<TValue>
{
    private readonly TValue _expected;
    private readonly IEqualityComparer<TValue>? _comparer;

    // Constructor 1: Just expected value
    public EqualsAssertion(
        AssertionContext<TValue> context,
        TValue expected)
        : base(context)
    {
        _expected = expected;
        _comparer = null;
    }

    // Constructor 2: Expected value with comparer
    public EqualsAssertion(
        AssertionContext<TValue> context,
        TValue expected,
        IEqualityComparer<TValue> comparer)
        : base(context)
    {
        _expected = expected;
        _comparer = comparer;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var comparer = _comparer ?? EqualityComparer<TValue>.Default;

        if (comparer.Equals(value!, _expected))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() => $"to be equal to {_expected}";
}
