using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is NOT equal to an expected value.
/// </summary>
public class NotEqualsAssertion<TValue> : Assertion<TValue>
{
    private readonly TValue _notExpected;
    private readonly IEqualityComparer<TValue>? _comparer;

    public NotEqualsAssertion(
        EvaluationContext<TValue> context,
        TValue notExpected,
        StringBuilder expressionBuilder,
        IEqualityComparer<TValue>? comparer = null)
        : base(context, expressionBuilder)
    {
        _notExpected = notExpected;
        _comparer = comparer;
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        var comparer = _comparer ?? EqualityComparer<TValue>.Default;

        if (!comparer.Equals(value!, _notExpected))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"both values are {value}"));
    }

    protected override string GetExpectation() => $"to not be equal to {_notExpected}";
}
