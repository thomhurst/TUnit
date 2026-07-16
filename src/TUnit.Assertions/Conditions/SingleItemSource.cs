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

    internal static SingleItemSource<TItem> Create<TValue>(
        AssertionContext<TValue> context,
        Func<TValue?, TItem?> mapper)
    {
        var itemContext = context.Map(mapper);
        itemContext.PreservePendingPreWorkOnMap = true;
        itemContext.SkipAssertionOnPreWorkFailure = true;
        return new SingleItemSource<TItem>(itemContext);
    }
}
