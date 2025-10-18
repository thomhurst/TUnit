using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for collection values.
/// This is the entry point for: Assert.That(collection)
/// Knows the TItem type, enabling better type inference for collection operations like IsInOrder, All, ContainsOnly.
/// Inherits from CollectionAssertionBase to get all collection-specific instance methods (Contains, IsInOrder, etc.)
/// that persist through And/Or continuations.
/// </summary>
public class CollectionAssertion<TCollection, TItem> : CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    public CollectionAssertion(TCollection value, string? expression)
        : base(new AssertionContext<TCollection>(value, CreateExpressionBuilder(expression)))
    {
    }

    private static StringBuilder CreateExpressionBuilder(string? expression)
    {
        var builder = new StringBuilder();
        builder.Append($"Assert.That({expression ?? "?"})");
        return builder;
    }
}
