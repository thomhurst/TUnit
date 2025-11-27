using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for asynchronous functions that return collections.
/// This is the entry point for: Assert.That(async () => await GetCollectionAsync())
/// Combines the lazy evaluation of AsyncFuncAssertion with the collection methods of CollectionAssertionBase.
/// Enables collection assertions like IsEmpty(), IsNotEmpty(), HasCount() on async lambda-wrapped collections.
/// </summary>
public class AsyncFuncCollectionAssertion<TItem> : CollectionAssertionBase<IEnumerable<TItem>, TItem>, IDelegateAssertionSource<IEnumerable<TItem>>
{
    public AsyncFuncCollectionAssertion(Func<Task<IEnumerable<TItem>?>> func, string? expression)
        : base(CreateContext(func, expression))
    {
    }

    private static AssertionContext<IEnumerable<TItem>> CreateContext(Func<Task<IEnumerable<TItem>?>> func, string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        var evaluationContext = new EvaluationContext<IEnumerable<TItem>>(async () =>
        {
            try
            {
                var result = await func().ConfigureAwait(false);
                return (result, null);
            }
            catch (Exception ex)
            {
                return (default, ex);
            }
        });
        return new AssertionContext<IEnumerable<TItem>>(evaluationContext, expressionBuilder);
    }

    /// <summary>
    /// Asserts that the function throws the specified exception type (or subclass).
    /// Example: await Assert.That(async () => await GetItemsAsync()).Throws&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsAssertion<TException> Throws<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        var mappedContext = Context.MapException<TException>();
        return new ThrowsAssertion<TException>(mappedContext!);
    }

    /// <summary>
    /// Asserts that the function throws exactly the specified exception type (not subclasses).
    /// Example: await Assert.That(async () => await GetItemsAsync()).ThrowsExactly&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsExactlyAssertion<TException> ThrowsExactly<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = Context.MapException<TException>();
        return new ThrowsExactlyAssertion<TException>(mappedContext!);
    }
}
