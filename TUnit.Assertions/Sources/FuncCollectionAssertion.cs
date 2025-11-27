using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for synchronous functions that return collections.
/// This is the entry point for: Assert.That(() => GetCollection())
/// Combines the lazy evaluation of FuncAssertion with the collection methods of CollectionAssertionBase.
/// Enables collection assertions like IsEmpty(), IsNotEmpty(), HasCount() on lambda-wrapped collections.
/// </summary>
public class FuncCollectionAssertion<TItem> : CollectionAssertionBase<IEnumerable<TItem>, TItem>, IDelegateAssertionSource<IEnumerable<TItem>>
{
    public FuncCollectionAssertion(Func<IEnumerable<TItem>?> func, string? expression)
        : base(CreateContext(func, expression))
    {
    }

    private static AssertionContext<IEnumerable<TItem>> CreateContext(Func<IEnumerable<TItem>?> func, string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        var evaluationContext = new EvaluationContext<IEnumerable<TItem>>(() =>
        {
            try
            {
                var result = func();
                return Task.FromResult<(IEnumerable<TItem>?, Exception?)>((result, null));
            }
            catch (Exception ex)
            {
                return Task.FromResult<(IEnumerable<TItem>?, Exception?)>((default, ex));
            }
        });
        return new AssertionContext<IEnumerable<TItem>>(evaluationContext, expressionBuilder);
    }

    /// <summary>
    /// Asserts that the function throws the specified exception type (or subclass).
    /// Example: await Assert.That(() => GetItems()).Throws&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsAssertion<TException> Throws<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        var mappedContext = Context.MapException<TException>();
        return new ThrowsAssertion<TException>(mappedContext!);
    }

    /// <summary>
    /// Asserts that the function throws exactly the specified exception type (not subclasses).
    /// Example: await Assert.That(() => GetItems()).ThrowsExactly&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsExactlyAssertion<TException> ThrowsExactly<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = Context.MapException<TException>();
        return new ThrowsExactlyAssertion<TException>(mappedContext!);
    }
}
