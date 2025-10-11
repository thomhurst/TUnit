using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for collection values.
/// This is the entry point for: Assert.That(collection)
/// Knows the TItem type, enabling better type inference for collection operations like IsInOrder, All, ContainsOnly.
/// Does not inherit from Assertion to prevent premature awaiting.
/// </summary>
public class CollectionAssertion<TItem> : IAssertionSource<IEnumerable<TItem>>
{
    public AssertionContext<IEnumerable<TItem>> Context { get; }

    public CollectionAssertion(IEnumerable<TItem> value, string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        Context = new AssertionContext<IEnumerable<TItem>>(value, expressionBuilder);
    }
}
