namespace TUnit.Assertions.Core;

/// <summary>
/// Base class for And/Or continuations that provides common initialization logic.
/// All continuations implement IAssertionSource to enable extension methods.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public abstract class ContinuationBase<TValue> : IAssertionSource<TValue>
{
    public AssertionContext<TValue> Context { get; }

    internal ContinuationBase(
        AssertionContext<TValue> context,
        Assertion<TValue> previousAssertion,
        string combinerExpression,
        CombinerType combinerType)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        context.ExpressionBuilder.Append(combinerExpression);
        // Set pending link state for next assertion to consume
        context.SetPendingLink(previousAssertion, combinerType);
    }
}
