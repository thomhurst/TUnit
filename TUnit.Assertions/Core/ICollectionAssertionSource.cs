namespace TUnit.Assertions.Core;

/// <summary>
/// Marker interface for collection assertion sources that exposes both the collection type and item type.
/// Extension methods targeting this interface can provide collection-specific operations like Contains, All, etc.
/// This interface enables type preservation through And/Or continuations while keeping all methods as extensions.
/// </summary>
/// <typeparam name="TCollection">The specific collection type</typeparam>
/// <typeparam name="TItem">The type of items in the collection</typeparam>
public interface ICollectionAssertionSource<TCollection, TItem> : IAssertionSource<TCollection>
    where TCollection : IEnumerable<TItem>
{
    // This is a marker interface - no additional members needed.
    // The magic is in the two type parameters which enable extension methods
    // to provide collection operations that know about both TCollection and TItem.
}
