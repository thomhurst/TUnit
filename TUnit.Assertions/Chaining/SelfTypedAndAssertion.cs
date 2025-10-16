using TUnit.Assertions.Core;

namespace TUnit.Assertions.Chaining;

/// <summary>
/// Represents an And combination of two self-typed assertions.
/// Both assertions must pass for the combined assertion to pass.
/// </summary>
internal class SelfTypedAndAssertion<TValue, TSelf> : SelfTypedAssertion<TValue, TSelf>
    where TSelf : SelfTypedAssertion<TValue, TSelf>
{
    private readonly TSelf _left;
    private readonly TSelf _right;

    public SelfTypedAndAssertion(TSelf left, TSelf right)
        : base(((SelfTypedAssertion<TValue, TSelf>)left).InternalContext)
    {
        _left = left;
        _right = right;
    }

    protected override string GetExpectation()
    {
        var leftExpectation = ((SelfTypedAssertion<TValue, TSelf>)_left).InternalGetExpectation();
        var rightExpectation = ((SelfTypedAssertion<TValue, TSelf>)_right).InternalGetExpectation();
        return $"({leftExpectation}) AND ({rightExpectation})";
    }

    public override async Task<TValue?> AssertAsync()
    {
        var leftResult = await ((SelfTypedAssertion<TValue, TSelf>)_left).ExecuteCoreAsync();
        var rightResult = await ((SelfTypedAssertion<TValue, TSelf>)_right).ExecuteCoreAsync();
        return rightResult;
    }
}
