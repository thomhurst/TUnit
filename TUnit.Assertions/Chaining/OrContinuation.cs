using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Core;

/// <summary>
/// Represents an Or continuation point in an assertion chain.
/// Returned by Assertion&lt;T&gt;.Or property to enable fluent chaining.
/// Implements IAssertionSource so all extension methods work automatically!
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public class OrContinuation<TValue> : IAssertionSource<TValue>
{
    /// <summary>
    /// The assertion context shared by all assertions in the chain.
    /// </summary>
    public AssertionContext<TValue> Context { get; }

    /// <summary>
    /// The previous assertion in the chain that could also pass.
    /// </summary>
    public Assertion<TValue> PreviousAssertion { get; }

    internal OrContinuation(AssertionContext<TValue> context, Assertion<TValue> previousAssertion)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        PreviousAssertion = previousAssertion ?? throw new ArgumentNullException(nameof(previousAssertion));
        Context.ExpressionBuilder.Append(".Or");

        // Set pending link state for next assertion to consume
        Context.SetPendingLink(previousAssertion, CombinerType.Or);
    }

    /// <summary>
    /// Asserts that the value is of the specified type and returns an assertion on the casted value.
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(value).IsNull().Or.IsTypeOf<List<string>>();
    /// </summary>
    public TypeOfAssertion<TValue, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TValue, TExpected>(Context);
    }

    /// <summary>
    /// Asserts that the value's type is assignable to the specified type.
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(value).IsNull().Or.IsAssignableTo<IDisposable>();
    /// </summary>
    public IsAssignableToAssertion<TTarget, TValue> IsAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, TValue>(Context);
    }

    /// <summary>
    /// Asserts that the value's type is NOT assignable to the specified type.
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(value).IsNull().Or.IsNotAssignableTo<IDisposable>();
    /// </summary>
    public IsNotAssignableToAssertion<TTarget, TValue> IsNotAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, TValue>(Context);
    }
}
