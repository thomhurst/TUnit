using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Base class for assertions that support custom equality comparers.
/// Provides common pattern for assertions using IEqualityComparer&lt;TItem&gt;.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
/// <typeparam name="TItem">The type of items being compared</typeparam>
public abstract class ComparerBasedAssertion<TValue, TItem> : Assertion<TValue>
{
    private IEqualityComparer<TItem>? _comparer;

    protected ComparerBasedAssertion(AssertionContext<TValue> context)
        : base(context)
    {
    }

    /// <summary>
    /// Specifies a custom equality comparer to use for item comparison.
    /// Protected method allows derived classes to override return type for fluent API.
    /// </summary>
    protected void SetComparer(IEqualityComparer<TItem> comparer)
    {
        _comparer = comparer;
        Context.ExpressionBuilder.Append($".Using({comparer.GetType().Name})");
    }

    /// <summary>
    /// Gets the comparer to use for item comparison.
    /// Returns the custom comparer if set, otherwise the default comparer.
    /// </summary>
    protected IEqualityComparer<TItem> GetComparer()
    {
        return _comparer ?? EqualityComparer<TItem>.Default;
    }

    /// <summary>
    /// Checks if a custom comparer has been specified.
    /// </summary>
    protected bool HasCustomComparer()
    {
        return _comparer != null;
    }
}
