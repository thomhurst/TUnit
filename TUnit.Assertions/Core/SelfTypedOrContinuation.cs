using TUnit.Assertions.Conditions;

namespace TUnit.Assertions.Core;

/// <summary>
/// Represents an Or continuation that preserves the source assertion type.
/// Enables instance methods from TSelf to remain available after .Or.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
/// <typeparam name="TSelf">The source assertion type to preserve</typeparam>
public class SelfTypedOrContinuation<TValue, TSelf> : IAssertionSource<TValue>
    where TSelf : SelfTypedAssertion<TValue, TSelf>
{
    /// <summary>
    /// The assertion context shared by all assertions in the chain.
    /// </summary>
    public AssertionContext<TValue> Context { get; }

    /// <summary>
    /// The previous assertion in the chain that could also pass.
    /// </summary>
    public TSelf PreviousAssertion { get; }

    internal SelfTypedOrContinuation(AssertionContext<TValue> context, TSelf previousAssertion)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        PreviousAssertion = previousAssertion ?? throw new ArgumentNullException(nameof(previousAssertion));
        Context.ExpressionBuilder.Append(".Or");

        // Set pending link state for next assertion to consume
        Context.SetPendingLinkSelfTyped(previousAssertion, CombinerType.Or);
    }

    /// <summary>
    /// Asserts that the value is of the specified type and returns an assertion on the casted value.
    /// </summary>
    public TypeOfAssertion<TValue, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TValue, TExpected>(Context);
    }

    /// <summary>
    /// Asserts that the value's type is assignable to the specified type.
    /// </summary>
    public IsAssignableToAssertion<TTarget, TValue> IsAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, TValue>(Context);
    }

    /// <summary>
    /// Asserts that the value's type is NOT assignable to the specified type.
    /// </summary>
    public IsNotAssignableToAssertion<TTarget, TValue> IsNotAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, TValue>(Context);
    }
}
