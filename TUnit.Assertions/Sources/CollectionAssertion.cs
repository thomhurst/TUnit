using System.Text;
using TUnit.Assertions.Conditions;
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

    /// <summary>
    /// Asserts that the value is of the specified type and returns an assertion on the casted value.
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(myCollection).IsTypeOf<List<string>>();
    /// </summary>
    public TypeOfAssertion<IEnumerable<TItem>, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<IEnumerable<TItem>, TExpected>(Context);
    }
}
