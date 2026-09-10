using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions;

public static class Fail
{
    /// <summary>
    /// Fails the current test.
    /// If called within Assert.Multiple(), the failure will be accumulated instead of thrown immediately.
    /// </summary>
    /// <param name="reason">The reason why the test failed</param>
    public static void Test(string reason)
    {
        var exception = new AssertionException(reason);
        var currentScope = AssertionScope.GetCurrentAssertionScope();

        if (currentScope != null)
        {
            // Within Assert.Multiple - accumulate exception instead of throwing
            currentScope.AddException(exception);
            return;
        }

        // No scope - throw immediately
        throw exception;
    }

    /// <summary>
    /// Fails the current test when the <paramref name="condition"/> is <c>false</c>.
    /// </summary>
    /// <param name="condition">When <c>false</c>, the test will be failed; otherwise it will continue to run</param>
    /// <param name="reason">The reason why the test was failed</param>
    public static void Unless([DoesNotReturnIf(false)] bool condition, string reason)
    {
        if (!condition)
        {
            Test(reason);
        }
    }

    /// <summary>
    /// Fails the current test when the <paramref name="condition"/> is <c>true</c>.
    /// </summary>
    /// <param name="condition">When <c>true</c>, the test will be failed; otherwise it will continue to run</param>
    /// <param name="reason">The reason why the test was failed</param>
    public static void When([DoesNotReturnIf(true)] bool condition, string reason)
    {
        if (condition)
        {
            Test(reason);
        }
    }
}
