using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is equal to an expected value.
/// Generic implementation that works for all types.
/// </summary>
public class EqualsAssertion<TValue> : Assertion<TValue>
{
    private readonly TValue _expected;
    private readonly IEqualityComparer<TValue>? _comparer;

    public EqualsAssertion(
        EvaluationContext<TValue> context,
        TValue expected,
        StringBuilder expressionBuilder,
        IEqualityComparer<TValue>? comparer = null)
        : base(context, expressionBuilder)
    {
        _expected = expected;
        _comparer = comparer;
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        var comparer = _comparer ?? EqualityComparer<TValue>.Default;

        if (comparer.Equals(value!, _expected))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() => $"to be equal to {_expected}";
}
