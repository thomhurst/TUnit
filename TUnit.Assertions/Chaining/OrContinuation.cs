using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Core;

/// <summary>
/// Represents an Or continuation point in an assertion chain.
/// Returned by Assertion&lt;T&gt;.Or property to enable fluent chaining.
/// Inherits from ValueAssertion to automatically expose all instance methods like IsTypeOf, IsAssignableTo, etc.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public class OrContinuation<TValue> : ValueAssertion<TValue>
{
    internal OrContinuation(AssertionContext<TValue> context, Assertion<TValue> previousAssertion)
        : base(context, previousAssertion, ".Or", CombinerType.Or)
    {
    }
}
