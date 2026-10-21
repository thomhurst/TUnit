// No namespace declaration: the wrapper below is in the global namespace.
// The trigger attribute is the local stand-in declared in CollectionShapeAssertionSource.cs.
using TUnit.Assertions.Core;

[TUnit.Assertions.Attributes.GenerateCollectionShapeAssertions]
public sealed class GlobalNamespaceCollectionShapeAssertionSource<T>
{
    public AssertionContext<T> Context { get; } = null!;
}
