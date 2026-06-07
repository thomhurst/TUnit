using System.Runtime.CompilerServices;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Assertions;

/// <summary>
/// Extension methods for asserting mock call verification through the TUnit assertion pipeline.
/// Enables: <c>await Assert.That(mock.Method()).WasCalled(Times.Once);</c>
/// </summary>
public static class MockAssertionExtensions
{
    /// <summary>
    /// Asserts that the mock member was called at least once.
    /// Generates the <c>WasCalled()</c> assertion on <see cref="IAssertionSource{ICallVerification}"/>.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to have been called at least once")]
    public static AssertionResult WasCalled<T>(T verification) where T : ICallVerification
    {
        return WasCalled(verification, Times.AtLeastOnce);
    }

    /// <summary>
    /// Asserts that the mock member was called the specified number of times.
    /// Generic overload for types implementing <see cref="ICallVerification"/> (e.g. PropertyMockCall).
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to have been called {times} times")]
    public static AssertionResult WasCalled<T>(
        this T verification,
        Times times) where T : ICallVerification
    {
        if (verification is null)
        {
            return AssertionResult.Failed("Verification target is null");
        }

        try
        {
            verification.WasCalled(times);
            return AssertionResult.Passed;
        }
        catch (MockVerificationException ex)
        {
            return AssertionResult.Failed(ex.Message, ex);
        }
    }

    /// <summary>
    /// Asserts that the mock member was never called.
    /// Generic overload for types implementing <see cref="ICallVerification"/> (e.g. PropertyMockCall).
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to not have been called")]
    public static AssertionResult WasNeverCalled<T>(
        this T verification) where T : ICallVerification
    {
        if (verification is null)
        {
            return AssertionResult.Failed("Verification target is null");
        }

        try
        {
            verification.WasNeverCalled();
            return AssertionResult.Passed;
        }
        catch (MockVerificationException ex)
        {
            return AssertionResult.Failed(ex.Message, ex);
        }
    }
}
