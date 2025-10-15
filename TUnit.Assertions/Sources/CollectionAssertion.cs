namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for collection values.
/// This is the entry point for: Assert.That(collection)
/// Knows the TItem type, enabling better type inference for collection operations like IsInOrder, All, ContainsOnly.
/// Inherits from ValueAssertion to get all value-based assertions like IsTypeOf without duplication.
/// </summary>
public class CollectionAssertion<TItem> : ValueAssertion<IEnumerable<TItem>>
{
    public CollectionAssertion(IEnumerable<TItem> value, string? expression)
        : base(value, expression)
    {
    }
}
