using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Core;

/// <summary>
/// And continuation for async enumerable assertions that preserves item type.
/// Inherits from AsyncEnumerableAssertionBase to automatically expose all async enumerable methods.
/// </summary>
public class AsyncEnumerableAndContinuation<TItem> : AsyncEnumerableAssertionBase<TItem>
{
    internal AsyncEnumerableAndContinuation(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        Assertion<IAsyncEnumerable<TItem>> previousAssertion)
        : base(context, previousAssertion, ".And", CombinerType.And)
    {
    }
}

/// <summary>
/// Or continuation for async enumerable assertions that preserves item type.
/// Inherits from AsyncEnumerableAssertionBase to automatically expose all async enumerable methods.
/// </summary>
public class AsyncEnumerableOrContinuation<TItem> : AsyncEnumerableAssertionBase<TItem>
{
    internal AsyncEnumerableOrContinuation(
        AssertionContext<IAsyncEnumerable<TItem>> context,
        Assertion<IAsyncEnumerable<TItem>> previousAssertion)
        : base(context, previousAssertion, ".Or", CombinerType.Or)
    {
    }
}
