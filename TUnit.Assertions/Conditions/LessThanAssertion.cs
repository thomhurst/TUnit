using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is less than a maximum value.
/// </summary>
public class LessThanAssertion<TValue> : Assertion<TValue>
    where TValue : IComparable<TValue>
{
    private readonly TValue _maximum;

    public LessThanAssertion(
        EvaluationContext<TValue> context,
        TValue maximum,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _maximum = maximum;
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value is null"));

        if (value.CompareTo(_maximum) < 0)
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() => $"to be less than {_maximum}";
}

/// <summary>
/// Asserts that a value is less than or equal to a maximum value.
/// </summary>
public class LessThanOrEqualAssertion<TValue> : Assertion<TValue>
    where TValue : IComparable<TValue>
{
    private readonly TValue _maximum;

    public LessThanOrEqualAssertion(
        EvaluationContext<TValue> context,
        TValue maximum,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _maximum = maximum;
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value is null"));

        if (value.CompareTo(_maximum) <= 0)
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() => $"to be less than or equal to {_maximum}";
}
