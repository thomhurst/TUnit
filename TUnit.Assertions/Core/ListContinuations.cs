using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Core;

/// <summary>
/// And continuation for list assertions that preserves list type and item type.
/// Inherits from ListAssertionBase to automatically expose all list and collection methods.
/// </summary>
public class ListAndContinuation<TList, TItem> : ListAssertionBase<TList, TItem>
    where TList : IList<TItem>
{
    internal ListAndContinuation(
        AssertionContext<TList> context,
        Assertion<TList> previousAssertion)
        : base(context, previousAssertion, ".And", CombinerType.And)
    {
    }
}

/// <summary>
/// Or continuation for list assertions that preserves list type and item type.
/// Inherits from ListAssertionBase to automatically expose all list and collection methods.
/// </summary>
public class ListOrContinuation<TList, TItem> : ListAssertionBase<TList, TItem>
    where TList : IList<TItem>
{
    internal ListOrContinuation(
        AssertionContext<TList> context,
        Assertion<TList> previousAssertion)
        : base(context, previousAssertion, ".Or", CombinerType.Or)
    {
    }
}
