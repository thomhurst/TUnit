using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for IAsyncEnumerable&lt;T&gt; assertions that provides collection-like operations.
/// Async enumerables are materialized (consumed) once during assertion evaluation.
/// </summary>
/// <typeparam name="TItem">The type of items in the async enumerable</typeparam>
public abstract class AsyncEnumerableAssertionBase<TItem> : Assertion<IAsyncEnumerable<TItem>>, IAssertionSource<IAsyncEnumerable<TItem>>
{
    protected AsyncEnumerableAssertionBase(AssertionContext<IAsyncEnumerable<TItem>> context)
        : base(context)
    {
    }

    /// <summary>
    /// Constructor for continuation classes (AsyncEnumerableAndContinuation, AsyncEnumerableOrContinuation).
    /// Handles linking to previous assertion and appending combiner expression.
    /// </summary>
    private protected AsyncEnumerableAssertionBase(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        Assertion<IAsyncEnumerable<TItem>> previousAssertion,
        string combinerExpression,
        CombinerType combinerType)
        : base(context)
    {
        context.ExpressionBuilder.Append(combinerExpression);
        context.SetPendingLink(previousAssertion, combinerType);
    }

    /// <summary>
    /// The assertion context, required by IAssertionSource.
    /// </summary>
    public new AssertionContext<IAsyncEnumerable<TItem>> Context => base.Context;

    protected override string GetExpectation() => "async enumerable assertion";

    /// <summary>
    /// Asserts that the async enumerable is empty.
    /// Example: await Assert.That(asyncEnumerable).IsEmpty();
    /// </summary>
    public AsyncEnumerableIsEmptyAssertion<TItem> IsEmpty()
    {
        Context.ExpressionBuilder.Append(".IsEmpty()");
        return new AsyncEnumerableIsEmptyAssertion<TItem>(Context, expectEmpty: true);
    }

    /// <summary>
    /// Asserts that the async enumerable is not empty.
    /// Example: await Assert.That(asyncEnumerable).IsNotEmpty();
    /// </summary>
    public AsyncEnumerableIsEmptyAssertion<TItem> IsNotEmpty()
    {
        Context.ExpressionBuilder.Append(".IsNotEmpty()");
        return new AsyncEnumerableIsEmptyAssertion<TItem>(Context, expectEmpty: false);
    }

    /// <summary>
    /// Asserts that the async enumerable has exactly the expected count of items.
    /// Example: await Assert.That(asyncEnumerable).HasCount(5);
    /// </summary>
    public AsyncEnumerableHasCountAssertion<TItem> HasCount(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".HasCount({expression})");
        return new AsyncEnumerableHasCountAssertion<TItem>(Context, expected);
    }

    /// <summary>
    /// Asserts that the async enumerable contains the expected item.
    /// Example: await Assert.That(asyncEnumerable).Contains(5);
    /// </summary>
    public AsyncEnumerableContainsAssertion<TItem> Contains(
        TItem expected,
        IEqualityComparer<TItem>? comparer = null,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Contains({expression})");
        return new AsyncEnumerableContainsAssertion<TItem>(Context, expected, comparer, expectContains: true);
    }

    /// <summary>
    /// Asserts that the async enumerable does not contain the expected item.
    /// Example: await Assert.That(asyncEnumerable).DoesNotContain(5);
    /// </summary>
    public AsyncEnumerableContainsAssertion<TItem> DoesNotContain(
        TItem expected,
        IEqualityComparer<TItem>? comparer = null,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".DoesNotContain({expression})");
        return new AsyncEnumerableContainsAssertion<TItem>(Context, expected, comparer, expectContains: false);
    }

    /// <summary>
    /// Asserts that all items in the async enumerable satisfy the predicate.
    /// Example: await Assert.That(asyncEnumerable).All(x => x > 0);
    /// </summary>
    public AsyncEnumerableAllAssertion<TItem> All(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".All({expression})");
        return new AsyncEnumerableAllAssertion<TItem>(Context, predicate);
    }

    /// <summary>
    /// Asserts that any item in the async enumerable satisfies the predicate.
    /// Example: await Assert.That(asyncEnumerable).Any(x => x > 10);
    /// </summary>
    public AsyncEnumerableAnyAssertion<TItem> Any(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Any({expression})");
        return new AsyncEnumerableAnyAssertion<TItem>(Context, predicate);
    }

    /// <summary>
    /// Returns an And continuation that preserves the async enumerable type.
    /// </summary>
    public new AsyncEnumerableAndContinuation<TItem> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<IAsyncEnumerable<TItem>>>();
            return new AsyncEnumerableAndContinuation<TItem>(Context, InternalWrappedExecution ?? this);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves the async enumerable type.
    /// </summary>
    public new AsyncEnumerableOrContinuation<TItem> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<IAsyncEnumerable<TItem>>>();
            return new AsyncEnumerableOrContinuation<TItem>(Context, InternalWrappedExecution ?? this);
        }
    }

    /// <inheritdoc />
    public TypeOfAssertion<IAsyncEnumerable<TItem>, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<IAsyncEnumerable<TItem>, TExpected>(Context);
    }

    /// <inheritdoc />
    public IsNotTypeOfAssertion<IAsyncEnumerable<TItem>, TExpected> IsNotTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotTypeOf<{typeof(TExpected).Name}>()");
        return new IsNotTypeOfAssertion<IAsyncEnumerable<TItem>, TExpected>(Context);
    }

    /// <inheritdoc />
    public IsAssignableToAssertion<TExpected, IAsyncEnumerable<TItem>> IsAssignableTo<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TExpected).Name}>()");
        return new IsAssignableToAssertion<TExpected, IAsyncEnumerable<TItem>>(Context);
    }

    /// <inheritdoc />
    public IsNotAssignableToAssertion<TExpected, IAsyncEnumerable<TItem>> IsNotAssignableTo<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TExpected).Name}>()");
        return new IsNotAssignableToAssertion<TExpected, IAsyncEnumerable<TItem>>(Context);
    }

    // CheckAsync is not overridden here - it's abstract in Assertion<T>
    // Each concrete assertion class will implement it
}
