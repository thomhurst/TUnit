using System.Runtime.CompilerServices;
using TUnit.Assertions.Core;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Assertions;

/// <summary>
/// Assertion that verifies a mock method was called the expected number of times.
/// Wraps the existing synchronous <see cref="ICallVerification.WasCalled(Times)"/> method
/// and converts <see cref="MockVerificationException"/> to <see cref="AssertionResult.Failed"/>.
/// </summary>
public class WasCalledAssertion : Assertion<ICallVerification>
{
    private readonly Times _times;

    public WasCalledAssertion(AssertionContext<ICallVerification> context, Times times)
        : base(context)
    {
        _times = times;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<ICallVerification> metadata)
    {
        if (metadata.Value is null)
        {
            return Task.FromResult(AssertionResult.Failed("Verification target is null"));
        }

        try
        {
            metadata.Value.WasCalled(_times);
            return Task.FromResult(AssertionResult.Passed);
        }
        catch (MockVerificationException ex)
        {
            return Task.FromResult(AssertionResult.Failed(ex.Message));
        }
    }

    protected override string GetExpectation()
        => $"to have been called {_times}";
}
