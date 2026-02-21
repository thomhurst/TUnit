using System.Runtime.CompilerServices;
using TUnit.Assertions.Core;
using TUnit.Mock.Exceptions;
using TUnit.Mock.Verification;

namespace TUnit.Mock.Assertions;

/// <summary>
/// Assertion that verifies a mock method was never called.
/// Wraps the existing synchronous <see cref="ICallVerification.WasNeverCalled()"/> method
/// and converts <see cref="MockVerificationException"/> to <see cref="AssertionResult.Failed"/>.
/// </summary>
public class WasNeverCalledAssertion : Assertion<ICallVerification>
{
    public WasNeverCalledAssertion(AssertionContext<ICallVerification> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<ICallVerification> metadata)
    {
        if (metadata.Value is null)
        {
            return Task.FromResult(AssertionResult.Failed("Verification target is null"));
        }

        try
        {
            metadata.Value.WasNeverCalled();
            return Task.FromResult(AssertionResult.Passed);
        }
        catch (MockVerificationException ex)
        {
            return Task.FromResult(AssertionResult.Failed(ex.Message));
        }
    }

    protected override string GetExpectation()
        => "to have never been called";
}
