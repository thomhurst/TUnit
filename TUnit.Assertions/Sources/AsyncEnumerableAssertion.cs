using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for IAsyncEnumerable&lt;T&gt; values.
/// This is the entry point for: Assert.That(asyncEnumerable) where asyncEnumerable is IAsyncEnumerable&lt;T&gt;.
/// Note: The async enumerable will be materialized (consumed) during assertion evaluation.
/// </summary>
public class AsyncEnumerableAssertion<TItem> : AsyncEnumerableAssertionBase<TItem>
{
    public AsyncEnumerableAssertion(IAsyncEnumerable<TItem> value, string? expression)
        : base(CreateContext(value, expression))
    {
    }

    internal AsyncEnumerableAssertion(AssertionContext<IAsyncEnumerable<TItem>> context)
        : base(context)
    {
    }

    private static AssertionContext<IAsyncEnumerable<TItem>> CreateContext(
        IAsyncEnumerable<TItem> value,
        string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        return new AssertionContext<IAsyncEnumerable<TItem>>(value, expressionBuilder);
    }
}
