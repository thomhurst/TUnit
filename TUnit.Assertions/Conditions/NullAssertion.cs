using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is null.
/// </summary>
public class NullAssertion<TValue> : Assertion<TValue>
{
    public NullAssertion(
        EvaluationContext<TValue> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (value == null)
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() => "to be null";
}

/// <summary>
/// Asserts that a value is not null.
/// </summary>
public class NotNullAssertion<TValue> : Assertion<TValue>
{
    public NotNullAssertion(
        EvaluationContext<TValue> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (value != null)
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed("value is null"));
    }

    protected override string GetExpectation() => "to not be null";
}

/// <summary>
/// Asserts that a value is not the default value for its type.
/// </summary>
public class IsNotDefaultAssertion<TValue> : Assertion<TValue>
{
    public IsNotDefaultAssertion(
        EvaluationContext<TValue> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (!EqualityComparer<TValue>.Default.Equals(value!, default!))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"value is default({typeof(TValue).Name})"));
    }

    protected override string GetExpectation() => $"to not be default({typeof(TValue).Name})";
}
