using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Assertion source for the item captured by a successful <c>HasSingleItem</c> assertion.
/// </summary>
/// <typeparam name="TItem">The collection item type.</typeparam>
[global::TUnit.Assertions.Attributes.GenerateCollectionShapeAssertions]
public sealed class SingleItemSource<TItem> : ValueAssertion<TItem>
{
    internal SingleItemSource(AssertionContext<TItem> context)
        : base(context)
    {
    }
}
