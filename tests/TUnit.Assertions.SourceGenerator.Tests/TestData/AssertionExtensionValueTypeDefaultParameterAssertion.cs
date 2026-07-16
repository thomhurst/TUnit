using System.Threading;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: <see cref="AssertionExtensionAttribute"/>-decorated class whose constructor
/// has a non-nullable value-type parameter declared with <c>= default</c>. The Roslyn-reported
/// default expression is <see langword="null"/>, but emitting <c>parameter = null</c> for a
/// value type is invalid C# (CS1750). The generator must render the bare <c>default</c>
/// literal, which the C# compiler infers as <c>default(TypeName)</c> from the parameter type.
/// </summary>
[AssertionExtension("RespectsToken")]
public class StringRespectsTokenAssertion : Assertion<string>
{
    private readonly CancellationToken _token;

    public StringRespectsTokenAssertion(AssertionContext<string> context, CancellationToken token = default)
        : base(context)
    {
        _token = token;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        return _token.IsCancellationRequested
            ? Task.FromResult(AssertionResult.Failed("token was canceled"))
            : Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to respect the supplied token";
}
