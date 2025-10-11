using System.Threading.Tasks;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Examples;

/// <summary>
/// Example assertion to demonstrate the AssertionExtension source generator.
/// This will auto-generate an IsPositive() extension method.
/// </summary>
[AssertionExtension("IsPositive")]
public class IsPositiveAssertion : Assertion<int>
{
    public IsPositiveAssertion(AssertionContext<int> context) : base(context) { }

    protected override string GetExpectation() => "to be positive";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
    {
        if (metadata.Value > 0)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"found {metadata.Value}"));
    }
}
