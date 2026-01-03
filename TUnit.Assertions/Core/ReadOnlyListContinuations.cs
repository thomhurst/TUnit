using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Core;

/// <summary>
/// And continuation for IReadOnlyList assertions that preserves the list type.
/// </summary>
public class ReadOnlyListAndContinuation<TList, TItem> : ReadOnlyListAssertionBase<TList, TItem>
    where TList : IReadOnlyList<TItem>
{
    public ReadOnlyListAndContinuation(
        AssertionContext<TList> context,
        Assertion<TList> previousAssertion)
        : base(context, previousAssertion, ".And", CombinerType.And)
    {
    }
}

/// <summary>
/// Or continuation for IReadOnlyList assertions that preserves the list type.
/// </summary>
public class ReadOnlyListOrContinuation<TList, TItem> : ReadOnlyListAssertionBase<TList, TItem>
    where TList : IReadOnlyList<TItem>
{
    public ReadOnlyListOrContinuation(
        AssertionContext<TList> context,
        Assertion<TList> previousAssertion)
        : base(context, previousAssertion, ".Or", CombinerType.Or)
    {
    }
}
