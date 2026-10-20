// No namespace declaration: everything below is in the global namespace.
using System.Threading.Tasks;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

[AssertionExtension("IsEmptyGlobal")]
public class GlobalStringIsEmptyAssertion : Assertion<string>
{
    public GlobalStringIsEmptyAssertion(AssertionContext<string> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        return Task.FromResult(string.IsNullOrEmpty(metadata.Value)
            ? AssertionResult.Passed
            : AssertionResult.Failed($"String was not empty: '{metadata.Value}'"));
    }

    protected override string GetExpectation() => "to be empty";
}
