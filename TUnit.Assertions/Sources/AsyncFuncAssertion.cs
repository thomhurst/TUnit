using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for asynchronous functions.
/// This is the entry point for: Assert.That(async () => await GetValueAsync())
/// Implements IDelegateAssertionSource to enable Throws() extension methods.
/// Does not inherit from Assertion to prevent premature awaiting.
/// </summary>
public class AsyncFuncAssertion<TValue> : IAssertionSource<TValue>, IDelegateAssertionSource<TValue>
{
    public EvaluationContext<TValue> Context { get; }
    public StringBuilder ExpressionBuilder { get; }

    public AsyncFuncAssertion(Func<Task<TValue>> func, string? expression)
    {
        Context = new EvaluationContext<TValue>(async () =>
        {
            try
            {
                var result = await func();
                return (result, null);
            }
            catch (Exception ex)
            {
                return (default(TValue), ex);
            }
        });
        ExpressionBuilder = new StringBuilder();
        ExpressionBuilder.Append($"Assert.That({expression ?? "?"})");
    }

    /// <summary>
    /// Asserts that the async function throws the specified exception type (or subclass).
    /// Instance method to avoid C# type inference issues with extension methods.
    /// Example: await Assert.That(async () => await ThrowingMethodAsync()).Throws&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsAssertion<TException> Throws<TException>() where TException : Exception
    {
        ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        var mappedContext = Context.Map<object?>(_ => null);
        return new ThrowsAssertion<TException>(mappedContext, ExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the async function throws exactly the specified exception type (not subclasses).
    /// Instance method to avoid C# type inference issues with extension methods.
    /// Example: await Assert.That(async () => await ThrowingMethodAsync()).ThrowsExactly&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsExactlyAssertion<TException> ThrowsExactly<TException>() where TException : Exception
    {
        ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = Context.Map<object?>(_ => null);
        return new ThrowsExactlyAssertion<TException>(mappedContext, ExpressionBuilder);
    }
}
