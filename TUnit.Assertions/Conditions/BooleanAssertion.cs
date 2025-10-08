using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a boolean value is true.
/// </summary>
public class TrueAssertion : Assertion<bool>
{
    public TrueAssertion(
        AssertionContext<bool> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<bool> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == true)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

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
        AssertionContext<bool> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<bool> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == false)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() => "to be false";
}
