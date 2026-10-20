// Regression test for https://github.com/thomhurst/TUnit/issues/7133.
// Intentionally has no namespace declaration: the assertion types below live in the global namespace,
// which used to make the assertion source generators emit `using <global namespace>;` and break the build.
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

[AssertionFrom<string>(nameof(string.IsNullOrEmpty), CustomName = "IsBlankGlobal_7133", ExpectationMessage = "be null or empty")]
public static partial class Issue7133GlobalStringAssertions
{
}

public static class Issue7133GlobalGenerateAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be positive")]
    public static bool IsPositiveGlobal_7133(this int value) => value > 0;
}

[AssertionExtension("IsEmptyGlobal_7133")]
public class Issue7133GlobalStringIsEmptyAssertion : Assertion<string>
{
    public Issue7133GlobalStringIsEmptyAssertion(AssertionContext<string> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        return Task.FromResult(string.IsNullOrEmpty(metadata.Value)
            ? AssertionResult.Passed
            : AssertionResult.Failed($"found '{metadata.Value}'"));
    }

    protected override string GetExpectation() => "to be empty";
}

public class Issue7133GlobalNamespaceTests
{
    [Test]
    public async Task AssertionFrom_declared_in_global_namespace_is_usable()
    {
        await Assert.That("").IsBlankGlobal_7133();
    }

    [Test]
    public async Task GenerateAssertion_declared_in_global_namespace_is_usable()
    {
        await Assert.That(1).IsPositiveGlobal_7133();
    }

    [Test]
    public async Task AssertionExtension_declared_in_global_namespace_is_usable()
    {
        await Assert.That("").IsEmptyGlobal_7133();
    }

    [Test]
    public async Task GenerateAssertion_declared_in_global_namespace_fails_when_condition_is_false()
    {
        await Assert.That(async () => await Assert.That(-1).IsPositiveGlobal_7133())
            .Throws<AssertionException>();
    }
}
