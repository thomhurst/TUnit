using TUnit.Assertions.Core;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Chaining;

/// <summary>
/// Represents an Or combination of two self-typed assertions.
/// At least one assertion must pass for the combined assertion to pass.
/// </summary>
internal class SelfTypedOrAssertion<TValue, TSelf> : SelfTypedAssertion<TValue, TSelf>
    where TSelf : SelfTypedAssertion<TValue, TSelf>
{
    private readonly TSelf _left;
    private readonly TSelf _right;

    public SelfTypedOrAssertion(TSelf left, TSelf right)
        : base(((SelfTypedAssertion<TValue, TSelf>)left).InternalContext)
    {
        _left = left;
        _right = right;
    }

    protected override string GetExpectation()
    {
        var leftExpectation = ((SelfTypedAssertion<TValue, TSelf>)_left).InternalGetExpectation();
        var rightExpectation = ((SelfTypedAssertion<TValue, TSelf>)_right).InternalGetExpectation();
        return $"({leftExpectation}) OR ({rightExpectation})";
    }

    public override async Task<TValue?> AssertAsync()
    {
        TValue? result = default;
        Exception? leftException = null;
        Exception? rightException = null;

        try
        {
            result = await ((SelfTypedAssertion<TValue, TSelf>)_left).ExecuteCoreAsync();
            return result;
        }
        catch (AssertionException ex)
        {
            leftException = ex;
        }

        try
        {
            result = await ((SelfTypedAssertion<TValue, TSelf>)_right).ExecuteCoreAsync();
            return result;
        }
        catch (AssertionException ex)
        {
            rightException = ex;
        }

        // Both failed, throw combined exception
        throw new AssertionException(
            $"Both assertions in Or chain failed:\n" +
            $"Left: {leftException?.Message}\n" +
            $"Right: {rightException?.Message}");
    }
}
