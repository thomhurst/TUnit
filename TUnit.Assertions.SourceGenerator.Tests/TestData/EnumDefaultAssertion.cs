using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Assertion with enum default parameter
/// Should generate extension method with properly formatted enum default
/// </summary>
[AssertionExtension("IsEqualToWithComparison")]
public class StringComparisonAssertion : Assertion<string>
{
    private readonly string _expected;
    private readonly StringComparison _comparison;

    public StringComparisonAssertion(
        AssertionContext<string> context,
        string expected,
        StringComparison comparison = StringComparison.Ordinal)
        : base(context)
    {
        _expected = expected;
        _comparison = comparison;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;

        if (string.Equals(value, _expected, _comparison))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"'{value}' does not equal '{_expected}' with {_comparison}"));
    }

    protected override string GetExpectation() => $"to equal '{_expected}' with {_comparison}";
}
