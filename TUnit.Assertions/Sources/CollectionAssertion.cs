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
    public EvaluationContext<IEnumerable<TItem>> Context { get; }
    public StringBuilder ExpressionBuilder { get; }

    public CollectionAssertion(IEnumerable<TItem> value, string? expression)
    {
        Context = new EvaluationContext<IEnumerable<TItem>>(value);
        ExpressionBuilder = new StringBuilder();
        ExpressionBuilder.Append($"Assert.That({expression ?? "?"})");
    }
}
