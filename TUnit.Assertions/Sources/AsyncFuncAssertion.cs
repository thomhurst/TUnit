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
    public AssertionContext<TValue> Context { get; }

    public AsyncFuncAssertion(Func<Task<TValue>> func, string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        var evaluationContext = new EvaluationContext<TValue>(async () =>
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
        Context = new AssertionContext<TValue>(evaluationContext, expressionBuilder);
    }

    /// <summary>
    /// Asserts that the async function throws the specified exception type (or subclass).
    /// Instance method to avoid C# type inference issues with extension methods.
    /// Example: await Assert.That(async () => await ThrowingMethodAsync()).Throws&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsAssertion<TException> Throws<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        var mappedContext = Context.Map<object?>(_ => null);
        return new ThrowsAssertion<TException>(mappedContext);
    }

    /// <summary>
    /// Asserts that the async function throws exactly the specified exception type (not subclasses).
    /// Instance method to avoid C# type inference issues with extension methods.
    /// Example: await Assert.That(async () => await ThrowingMethodAsync()).ThrowsExactly&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsExactlyAssertion<TException> ThrowsExactly<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = Context.Map<object?>(_ => null);
        return new ThrowsExactlyAssertion<TException>(mappedContext);
    }
}
