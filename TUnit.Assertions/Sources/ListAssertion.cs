using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for IList&lt;T&gt; values.
/// This is the entry point for: Assert.That(list) where list is IList&lt;T&gt;.
/// Provides index-based methods (HasItemAt, ItemAt, FirstItem, LastItem)
/// in addition to all standard collection methods.
/// </summary>
public class ListAssertion<TItem> : ListAssertionBase<IList<TItem>, TItem>
{
    public ListAssertion(IList<TItem> value, string? expression)
        : base(CreateContext(value, expression))
    {
    }

    internal ListAssertion(AssertionContext<IList<TItem>> context)
        : base(context)
    {
    }

    private static AssertionContext<IList<TItem>> CreateContext(
        IList<TItem> value,
        string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        return new AssertionContext<IList<TItem>>(value, expressionBuilder);
    }
}
