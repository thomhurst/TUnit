using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Assertion with negated method name
/// Should generate both positive and negative extension methods
/// </summary>
[AssertionExtension("IsTrue", NegatedMethodName = "IsFalse")]
public class TrueAssertion : Assertion<bool>
{
    public TrueAssertion(AssertionContext<bool> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<bool> metadata)
    {
        var value = metadata.Value;

        if (value)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed("value was false"));
    }

    protected override string GetExpectation() => "to be true";
}
