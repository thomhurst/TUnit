using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for synchronous functions.
/// This is the entry point for: Assert.That(() => GetValue())
/// Implements IDelegateAssertionSource to enable Throws() extension methods.
/// </summary>
public class FuncAssertion<TValue> : Assertion<TValue>, IDelegateAssertionSource<TValue>
{
    public FuncAssertion(Func<TValue> func, string? expression)
        : base(new EvaluationContext<TValue>(async () =>
        {
            try
            {
                // Run on thread pool to avoid blocking
                var result = await Task.Run(func);
                return (result, null);
            }
            catch (Exception ex)
            {
                return (default(TValue), ex);
            }
        }))
    {
        ExpressionBuilder.Append($"Assert.That({expression ?? "?"})");
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        // Source assertions don't perform checks
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to evaluate successfully";

    /// <summary>
    /// Asserts that the function throws the specified exception type (or subclass).
    /// Instance method to avoid C# type inference issues with extension methods.
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsAssertion<TException> Throws<TException>() where TException : Exception
    {
        ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        var mappedContext = Context.Map<object?>(_ => null);
        return new ThrowsAssertion<TException>(mappedContext, ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the function throws exactly the specified exception type (not subclasses).
    /// Instance method to avoid C# type inference issues with extension methods.
    /// Example: await Assert.That(() => ThrowingMethod()).ThrowsExactly&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsExactlyAssertion<TException> ThrowsExactly<TException>() where TException : Exception
    {
        ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = Context.Map<object?>(_ => null);
        return new ThrowsExactlyAssertion<TException>(mappedContext, ExpressionBuilder);
    }
}
