using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Non-generic assertion class
/// Should generate extension method without generic parameters
/// </summary>
[AssertionExtension("IsEmpty")]
public class StringIsEmptyAssertion : Assertion<string>
{
    public StringIsEmptyAssertion(AssertionContext<string> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;

        if (string.IsNullOrEmpty(value))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"String was not empty: '{value}'"));
    }

    protected override string GetExpectation() => "to be empty";
}
