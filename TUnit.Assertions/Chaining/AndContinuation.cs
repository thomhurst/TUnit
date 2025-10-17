using TUnit.Assertions.Core;

namespace TUnit.Assertions.Core;

/// <summary>
/// Represents an And continuation point in an assertion chain.
/// Returned by Assertion&lt;T&gt;.And property to enable fluent chaining.
/// Implements IAssertionSource so all extension methods work automatically.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public class AndContinuation<TValue> : ContinuationBase<TValue>
{
    internal AndContinuation(AssertionContext<TValue> context, Assertion<TValue> previousAssertion)
        : base(context, previousAssertion, ".And", CombinerType.And)
    {
    }
}
