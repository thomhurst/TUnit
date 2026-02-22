using System.Runtime.CompilerServices;
using TUnit.Assertions.Core;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Assertions;

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
    /// Asserts that the mock member was called the specified number of times.
    /// Generic overload for types implementing <see cref="ICallVerification"/> (e.g. PropertyVerifyAccessor).
    /// </summary>
    public static WasCalledAssertion WasCalled<T>(
        this IAssertionSource<T> source,
        Times times,
        [CallerArgumentExpression(nameof(times))] string? expression = null) where T : ICallVerification
    {
        source.Context.ExpressionBuilder.Append($".WasCalled({expression})");
        var mappedContext = source.Context.Map<ICallVerification>(v => v);
        return new WasCalledAssertion(mappedContext, times);
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

    /// <summary>
    /// Asserts that the mock member was never called.
    /// Generic overload for types implementing <see cref="ICallVerification"/> (e.g. PropertyVerifyAccessor).
    /// </summary>
    public static WasNeverCalledAssertion WasNeverCalled<T>(
        this IAssertionSource<T> source) where T : ICallVerification
    {
        source.Context.ExpressionBuilder.Append(".WasNeverCalled()");
        var mappedContext = source.Context.Map<ICallVerification>(v => v);
        return new WasNeverCalledAssertion(mappedContext);
    }
}
