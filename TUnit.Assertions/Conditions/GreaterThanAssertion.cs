using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is greater than a minimum value.
/// Works for any comparable type.
/// </summary>
public class GreaterThanAssertion<TValue> : Assertion<TValue>
    where TValue : IComparable<TValue>
{
    private readonly TValue _minimum;

    public GreaterThanAssertion(
        EvaluationContext<TValue> context,
        TValue minimum,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _minimum = minimum;
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value is null"));

        if (value.CompareTo(_minimum) > 0)
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() => $"to be greater than {_minimum}";
}

/// <summary>
/// Asserts that a value is greater than or equal to a minimum value.
/// </summary>
public class GreaterThanOrEqualAssertion<TValue> : Assertion<TValue>
    where TValue : IComparable<TValue>
{
    private readonly TValue _minimum;

    public GreaterThanOrEqualAssertion(
        EvaluationContext<TValue> context,
        TValue minimum,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _minimum = minimum;
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value is null"));

        if (value.CompareTo(_minimum) >= 0)
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() => $"to be greater than or equal to {_minimum}";
}
