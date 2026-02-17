#if NET5_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Adapters;
using TUnit.Assertions.Collections;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for Memory and ReadOnlyMemory assertions.
/// Provides collection-like assertion methods using the adapter pattern.
/// </summary>
/// <typeparam name="TMemory">The memory type (Memory&lt;TItem&gt; or ReadOnlyMemory&lt;TItem&gt;).</typeparam>
/// <typeparam name="TItem">The type of items in the memory.</typeparam>
public abstract class MemoryAssertionBase<TMemory, TItem> : Assertion<TMemory>, IAssertionSource<TMemory>
{
    /// <summary>
    /// Explicit implementation of IAssertionSource.Context to expose the context publicly.
    /// </summary>
    AssertionContext<TMemory> IAssertionSource<TMemory>.Context => Context;

    /// <summary>
    /// Factory function to create an adapter from the memory value.
    /// </summary>
    protected abstract ICollectionAdapter<TItem> CreateAdapter(TMemory value);

    protected MemoryAssertionBase(AssertionContext<TMemory> context)
        : base(context)
    {
    }

    /// <summary>
    /// Constructor for continuation classes.
    /// </summary>
    private protected MemoryAssertionBase(
        AssertionContext<TMemory> context,
        Assertion<TMemory> previousAssertion,
        string combinerExpression,
        CombinerType combinerType)
        : base(context)
    {
        context.ExpressionBuilder.Append(combinerExpression);
        context.SetPendingLink(previousAssertion, combinerType);
    }

    protected override string GetExpectation() => "memory assertion";

    // Type assertions
    public TypeOfAssertion<TMemory, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TMemory, TExpected>(Context);
    }

    public IsAssignableToAssertion<TTarget, TMemory> IsAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, TMemory>(Context);
    }

    public IsNotAssignableToAssertion<TTarget, TMemory> IsNotAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, TMemory>(Context);
    }

    public IsAssignableFromAssertion<TTarget, TMemory> IsAssignableFrom<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableFrom<{typeof(TTarget).Name}>()");
        return new IsAssignableFromAssertion<TTarget, TMemory>(Context);
    }

    public IsNotAssignableFromAssertion<TTarget, TMemory> IsNotAssignableFrom<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableFrom<{typeof(TTarget).Name}>()");
        return new IsNotAssignableFromAssertion<TTarget, TMemory>(Context);
    }

    public IsNotTypeOfAssertion<TMemory, TExpected> IsNotTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotTypeOf<{typeof(TExpected).Name}>()");
        return new IsNotTypeOfAssertion<TMemory, TExpected>(Context);
    }

    // Collection methods

    /// <summary>
    /// Asserts that the memory is empty.
    /// </summary>
    public MemoryIsEmptyAssertion<TMemory, TItem> IsEmpty()
    {
        Context.ExpressionBuilder.Append(".IsEmpty()");
        return new MemoryIsEmptyAssertion<TMemory, TItem>(Context, CreateAdapter);
    }

    /// <summary>
    /// Asserts that the memory is not empty.
    /// </summary>
    public MemoryIsNotEmptyAssertion<TMemory, TItem> IsNotEmpty()
    {
        Context.ExpressionBuilder.Append(".IsNotEmpty()");
        return new MemoryIsNotEmptyAssertion<TMemory, TItem>(Context, CreateAdapter);
    }

    /// <summary>
    /// Asserts that the memory contains the expected item.
    /// </summary>
    public MemoryContainsAssertion<TMemory, TItem> Contains(
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Contains({expression})");
        return new MemoryContainsAssertion<TMemory, TItem>(Context, CreateAdapter, expected);
    }

    /// <summary>
    /// Asserts that the memory does not contain the expected item.
    /// </summary>
    public MemoryDoesNotContainAssertion<TMemory, TItem> DoesNotContain(
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".DoesNotContain({expression})");
        return new MemoryDoesNotContainAssertion<TMemory, TItem>(Context, CreateAdapter, expected);
    }

    /// <summary>
    /// Gets the count for further numeric assertions.
    /// </summary>
    public MemoryCountSource<TMemory, TItem> Count()
    {
        Context.ExpressionBuilder.Append(".Count()");
        return new MemoryCountSource<TMemory, TItem>(Context, CreateAdapter);
    }

    /// <summary>
    /// Asserts that the memory has exactly one item.
    /// </summary>
    public MemoryHasSingleItemAssertion<TMemory, TItem> HasSingleItem()
    {
        Context.ExpressionBuilder.Append(".HasSingleItem()");
        return new MemoryHasSingleItemAssertion<TMemory, TItem>(Context, CreateAdapter);
    }

    /// <summary>
    /// Asserts that all items satisfy the predicate.
    /// </summary>
    public MemoryAllAssertion<TMemory, TItem> All(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".All({expression})");
        return new MemoryAllAssertion<TMemory, TItem>(Context, CreateAdapter, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that any item satisfies the predicate.
    /// </summary>
    public MemoryAnyAssertion<TMemory, TItem> Any(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Any({expression})");
        return new MemoryAnyAssertion<TMemory, TItem>(Context, CreateAdapter, predicate);
    }

    /// <summary>
    /// Asserts that the memory is in ascending order.
    /// </summary>
    public MemoryIsInOrderAssertion<TMemory, TItem> IsInOrder()
    {
        Context.ExpressionBuilder.Append(".IsInOrder()");
        return new MemoryIsInOrderAssertion<TMemory, TItem>(Context, CreateAdapter);
    }

    /// <summary>
    /// Asserts that the memory is in descending order.
    /// </summary>
    public MemoryIsInDescendingOrderAssertion<TMemory, TItem> IsInDescendingOrder()
    {
        Context.ExpressionBuilder.Append(".IsInDescendingOrder()");
        return new MemoryIsInDescendingOrderAssertion<TMemory, TItem>(Context, CreateAdapter);
    }

    /// <summary>
    /// Asserts that all items in the memory are distinct.
    /// </summary>
    public MemoryHasDistinctItemsAssertion<TMemory, TItem> HasDistinctItems()
    {
        Context.ExpressionBuilder.Append(".HasDistinctItems()");
        return new MemoryHasDistinctItemsAssertion<TMemory, TItem>(Context, CreateAdapter);
    }

    // And/Or continuations for chaining
    public new MemoryAndContinuation<TMemory, TItem> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<TMemory>>();
            return new MemoryAndContinuation<TMemory, TItem>(Context, InternalWrappedExecution ?? this, CreateAdapter);
        }
    }

    public new MemoryOrContinuation<TMemory, TItem> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<TMemory>>();
            return new MemoryOrContinuation<TMemory, TItem>(Context, InternalWrappedExecution ?? this, CreateAdapter);
        }
    }
}

/// <summary>
/// And continuation for memory assertions.
/// </summary>
public class MemoryAndContinuation<TMemory, TItem> : MemoryAssertionBase<TMemory, TItem>
{
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;

    internal MemoryAndContinuation(
        AssertionContext<TMemory> context,
        Assertion<TMemory> previous,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory)
        : base(context, previous, ".And", CombinerType.And)
    {
        _adapterFactory = adapterFactory;
    }

    protected override ICollectionAdapter<TItem> CreateAdapter(TMemory value) => _adapterFactory(value);
}

/// <summary>
/// Or continuation for memory assertions.
/// </summary>
public class MemoryOrContinuation<TMemory, TItem> : MemoryAssertionBase<TMemory, TItem>
{
    private readonly Func<TMemory, ICollectionAdapter<TItem>> _adapterFactory;

    internal MemoryOrContinuation(
        AssertionContext<TMemory> context,
        Assertion<TMemory> previous,
        Func<TMemory, ICollectionAdapter<TItem>> adapterFactory)
        : base(context, previous, ".Or", CombinerType.Or)
    {
        _adapterFactory = adapterFactory;
    }

    protected override ICollectionAdapter<TItem> CreateAdapter(TMemory value) => _adapterFactory(value);
}
#endif
