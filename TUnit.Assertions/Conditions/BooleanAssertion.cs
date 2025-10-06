using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a boolean value is true.
/// </summary>
public class TrueAssertion : Assertion<bool>
{
    public TrueAssertion(
        EvaluationContext<bool> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(bool value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == true)
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() => "to be true";
}

/// <summary>
/// Asserts that a boolean value is false.
/// </summary>
public class FalseAssertion : Assertion<bool>
{
    public FalseAssertion(
        EvaluationContext<bool> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(bool value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == false)
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() => "to be false";
}
