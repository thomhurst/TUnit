using TUnit.Assertions.Core;

namespace TUnit.Assertions.Core;

/// <summary>
/// Represents an Or continuation point in an assertion chain.
/// Returned by Assertion&lt;T&gt;.Or property to enable fluent chaining.
/// Implements IAssertionSource so all extension methods work automatically.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public class OrContinuation<TValue> : IAssertionSource<TValue>
{
    public AssertionContext<TValue> Context { get; }

    internal OrContinuation(AssertionContext<TValue> context, Assertion<TValue> previousAssertion)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        context.ExpressionBuilder.Append(".Or");
        // Set pending link state for next assertion to consume
        context.SetPendingLink(previousAssertion, CombinerType.Or);
    }
}
