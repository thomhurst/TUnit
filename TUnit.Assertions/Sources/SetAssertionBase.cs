using System.Runtime.CompilerServices;
using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Adapters;
using TUnit.Assertions.Collections;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Conditions.Wrappers;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for set assertions that preserves type through And/Or chains.
/// Inherits from CollectionAssertionBase to automatically get all collection methods.
/// Adds set-specific methods like IsSubsetOf, IsSupersetOf, Overlaps, etc.
/// </summary>
/// <typeparam name="TSet">The set type (e.g., HashSet, ISet, IReadOnlySet)</typeparam>
/// <typeparam name="TItem">The item type in the set</typeparam>
public abstract class SetAssertionBase<TSet, TItem> : CollectionAssertionBase<TSet, TItem>
    where TSet : IEnumerable<TItem>
{
    /// <summary>
    /// Creates a set adapter from the set value.
    /// Must be implemented by derived classes to provide the appropriate adapter.
    /// </summary>
    protected abstract ISetAdapter<TItem> CreateSetAdapter(TSet value);

    protected SetAssertionBase(AssertionContext<TSet> context)
        : base(context)
    {
    }

    /// <summary>
    /// Constructor for continuation classes (SetAndContinuation, SetOrContinuation).
    /// Handles linking to previous assertion and appending combiner expression.
    /// </summary>
    private protected SetAssertionBase(
        AssertionContext<TSet> context,
        Assertion<TSet> previousAssertion,
        string combinerExpression,
        CombinerType combinerType)
        : base(context, previousAssertion, combinerExpression, combinerType)
    {
    }

    protected override string GetExpectation() => "set assertion";

    // ========================================
    // Set-specific methods
    // ========================================

    /// <summary>
    /// Asserts that the set is a subset of the specified collection.
    /// Example: await Assert.That(set).IsSubsetOf(otherSet);
    /// </summary>
    public SetIsSubsetOfAssertion<TSet, TItem> IsSubsetOf(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsSubsetOf({expression})");
        return new SetIsSubsetOfAssertion<TSet, TItem>(Context, CreateSetAdapter, other);
    }

    /// <summary>
    /// Asserts that the set is a superset of the specified collection.
    /// Example: await Assert.That(set).IsSupersetOf(otherSet);
    /// </summary>
    public SetIsSupersetOfAssertion<TSet, TItem> IsSupersetOf(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsSupersetOf({expression})");
        return new SetIsSupersetOfAssertion<TSet, TItem>(Context, CreateSetAdapter, other);
    }

    /// <summary>
    /// Asserts that the set is a proper subset of the specified collection.
    /// A proper subset is a subset that is not equal to the other collection.
    /// Example: await Assert.That(set).IsProperSubsetOf(otherSet);
    /// </summary>
    public SetIsProperSubsetOfAssertion<TSet, TItem> IsProperSubsetOf(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsProperSubsetOf({expression})");
        return new SetIsProperSubsetOfAssertion<TSet, TItem>(Context, CreateSetAdapter, other);
    }

    /// <summary>
    /// Asserts that the set is a proper superset of the specified collection.
    /// A proper superset is a superset that is not equal to the other collection.
    /// Example: await Assert.That(set).IsProperSupersetOf(otherSet);
    /// </summary>
    public SetIsProperSupersetOfAssertion<TSet, TItem> IsProperSupersetOf(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsProperSupersetOf({expression})");
        return new SetIsProperSupersetOfAssertion<TSet, TItem>(Context, CreateSetAdapter, other);
    }

    /// <summary>
    /// Asserts that the set overlaps with the specified collection (has at least one common element).
    /// Example: await Assert.That(set).Overlaps(otherSet);
    /// </summary>
    public SetOverlapsAssertion<TSet, TItem> Overlaps(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Overlaps({expression})");
        return new SetOverlapsAssertion<TSet, TItem>(Context, CreateSetAdapter, other);
    }

    /// <summary>
    /// Asserts that the set does not overlap with the specified collection (has no common elements).
    /// Example: await Assert.That(set).DoesNotOverlap(otherSet);
    /// </summary>
    public SetDoesNotOverlapAssertion<TSet, TItem> DoesNotOverlap(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".DoesNotOverlap({expression})");
        return new SetDoesNotOverlapAssertion<TSet, TItem>(Context, CreateSetAdapter, other);
    }

    /// <summary>
    /// Asserts that the set equals the specified collection (contains exactly the same elements).
    /// Example: await Assert.That(set).SetEquals(otherSet);
    /// </summary>
    public SetEqualsAssertion<TSet, TItem> SetEquals(
        IEnumerable<TItem> other,
        [CallerArgumentExpression(nameof(other))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".SetEquals({expression})");
        return new SetEqualsAssertion<TSet, TItem>(Context, CreateSetAdapter, other);
    }

    /// <summary>
    /// Returns an And continuation that preserves set type and item type.
    /// </summary>
    public new SetAndContinuation<TSet, TItem> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<TSet>>();
            return new SetAndContinuation<TSet, TItem>(Context, InternalWrappedExecution ?? this, CreateSetAdapter);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves set type and item type.
    /// </summary>
    public new SetOrContinuation<TSet, TItem> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<TSet>>();
            return new SetOrContinuation<TSet, TItem>(Context, InternalWrappedExecution ?? this, CreateSetAdapter);
        }
    }
}

/// <summary>
/// And continuation for set assertions.
/// </summary>
public class SetAndContinuation<TSet, TItem> : SetAssertionBase<TSet, TItem>
    where TSet : IEnumerable<TItem>
{
    private readonly Func<TSet, ISetAdapter<TItem>> _adapterFactory;

    internal SetAndContinuation(
        AssertionContext<TSet> context,
        Assertion<TSet> previous,
        Func<TSet, ISetAdapter<TItem>> adapterFactory)
        : base(context, previous, ".And", CombinerType.And)
    {
        _adapterFactory = adapterFactory;
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(TSet value) => _adapterFactory(value);
}

/// <summary>
/// Or continuation for set assertions.
/// </summary>
public class SetOrContinuation<TSet, TItem> : SetAssertionBase<TSet, TItem>
    where TSet : IEnumerable<TItem>
{
    private readonly Func<TSet, ISetAdapter<TItem>> _adapterFactory;

    internal SetOrContinuation(
        AssertionContext<TSet> context,
        Assertion<TSet> previous,
        Func<TSet, ISetAdapter<TItem>> adapterFactory)
        : base(context, previous, ".Or", CombinerType.Or)
    {
        _adapterFactory = adapterFactory;
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(TSet value) => _adapterFactory(value);
}
