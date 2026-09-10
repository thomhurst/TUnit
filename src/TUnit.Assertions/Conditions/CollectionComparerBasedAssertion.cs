using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Base class for collection assertions that support custom equality comparers.
/// Inherits from CollectionAssertionBase to ensure And/Or continuations preserve collection type awareness.
/// </summary>
/// <typeparam name="TCollection">The concrete collection type</typeparam>
/// <typeparam name="TItem">The type of items being compared</typeparam>
public abstract class CollectionComparerBasedAssertion<TCollection, TItem> : CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    protected IEqualityComparer<TItem>? Comparer;

    protected CollectionComparerBasedAssertion(AssertionContext<TCollection> context)
        : base(context)
    {
    }

    /// <summary>
    /// Specifies a custom equality comparer to use for item comparison.
    /// Protected method allows derived classes to override return type for fluent API.
    /// </summary>
    protected void SetComparer(IEqualityComparer<TItem> comparer)
    {
        Comparer = comparer;
        Context.ExpressionBuilder.Append($".Using({comparer.GetType().Name})");
    }

    /// <summary>
    /// Gets the comparer to use for item comparison.
    /// Returns the custom comparer if set, otherwise the default comparer.
    /// </summary>
    protected IEqualityComparer<TItem> GetComparer()
    {
        return Comparer ?? EqualityComparer<TItem>.Default;
    }
}
