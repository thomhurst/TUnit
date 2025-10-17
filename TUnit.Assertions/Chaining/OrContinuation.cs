using TUnit.Assertions.Core;

namespace TUnit.Assertions.Core;

/// <summary>
/// Represents an Or continuation point in an assertion chain.
/// Returned by Assertion&lt;T&gt;.Or property to enable fluent chaining.
/// Implements IAssertionSource so all extension methods work automatically.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public class OrContinuation<TValue> : ContinuationBase<TValue>
{
    internal OrContinuation(AssertionContext<TValue> context, Assertion<TValue> previousAssertion)
        : base(context, previousAssertion, ".Or", CombinerType.Or)
    {
    }
}
