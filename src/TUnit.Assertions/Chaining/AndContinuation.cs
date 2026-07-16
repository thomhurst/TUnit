using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Core;

/// <summary>
/// Represents an And continuation point in an assertion chain.
/// Returned by Assertion&lt;T&gt;.And property to enable fluent chaining.
/// Inherits from ValueAssertion to automatically expose all instance methods like IsTypeOf, IsAssignableTo, etc.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public class AndContinuation<TValue> : ValueAssertion<TValue>
{
    internal AndContinuation(AssertionContext<TValue> context, Assertion<TValue> previousAssertion)
        : base(context, previousAssertion, ".And", CombinerType.And)
    {
    }
}
