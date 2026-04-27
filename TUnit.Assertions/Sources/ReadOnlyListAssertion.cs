using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Assertion class for IReadOnlyList&lt;T&gt; that provides collection and index-based operations.
/// </summary>
/// <typeparam name="TItem">The type of items in the read-only list</typeparam>
public class ReadOnlyListAssertion<TItem> : ReadOnlyListAssertionBase<IReadOnlyList<TItem>, TItem>
#if !NETSTANDARD2_0
    , IAssertionSourceFor<IReadOnlyList<TItem>, ReadOnlyListAssertion<TItem>>
#endif
{
    public ReadOnlyListAssertion(IReadOnlyList<TItem>? value, string? expression)
        : base(CreateContext(value, expression))
    {
    }

    internal ReadOnlyListAssertion(AssertionContext<IReadOnlyList<TItem>> context)
        : base(context)
    {
    }

#if !NETSTANDARD2_0
    public static ReadOnlyListAssertion<TItem> Create(IReadOnlyList<TItem> item, string label) => new(item, label);
#endif

    private static AssertionContext<IReadOnlyList<TItem>> CreateContext(
        IReadOnlyList<TItem>? value,
        string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        return new AssertionContext<IReadOnlyList<TItem>>(value!, expressionBuilder);
    }
}
