using System.Runtime.CompilerServices;
using TUnit.Assertions.Core;
using TUnit.Mock.Verification;

namespace TUnit.Mock.Assertions;

/// <summary>
/// Extension methods for asserting mock call verification through the TUnit assertion pipeline.
/// Enables: <c>await Assert.That(mock.Verify.Method()).WasCalled(Times.Once);</c>
/// </summary>
public static class MockAssertionExtensions
{
    /// <summary>
    /// Asserts that the mock member was called the specified number of times.
    /// </summary>
    public static WasCalledAssertion WasCalled(
        this IAssertionSource<ICallVerification> source,
        Times times,
        [CallerArgumentExpression(nameof(times))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".WasCalled({expression})");
        return new WasCalledAssertion(source.Context, times);
    }

    /// <summary>
    /// Asserts that the mock member was never called.
    /// </summary>
    public static WasNeverCalledAssertion WasNeverCalled(
        this IAssertionSource<ICallVerification> source)
    {
        source.Context.ExpressionBuilder.Append(".WasNeverCalled()");
        return new WasNeverCalledAssertion(source.Context);
    }
}
