using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;
using TUnit.Assertions.Should;
using TUnit.Assertions.Should.Extensions;

namespace TUnit.Assertions.Should.Tests;

[AssertionExtension("IsOdd")]
public sealed class OddAssertion : Assertion<int>
{
    public OddAssertion(AssertionContext<int> context) : base(context) { }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
        => Task.FromResult(metadata.Value % 2 != 0
            ? AssertionResult.Passed
            : AssertionResult.Failed($"{metadata.Value} is even"));

    protected override string GetExpectation() => "to be odd";
}

public class UserDefinedAssertionTests
{
    [Test]
    public async Task User_assertion_gets_Should_flavored_counterpart()
    {
        // OddAssertion is declared in this test assembly — the generator running in
        // user compilation must emit ShouldOddAssertionExtensions.BeOdd, even though
        // the baked-into-Should.dll extensions for built-in assertions are skipped.
        await 3.Should().BeOdd();
    }
}
