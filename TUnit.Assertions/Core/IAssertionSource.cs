using TUnit.Assertions.Conditions;

namespace TUnit.Assertions.Core;

/// <summary>
/// Non-generic base interface for all assertion sources.
/// Used for extension methods that need single type parameter (like IsTypeOf).
/// </summary>
public interface IAssertionSource
{
}

/// <summary>
/// Common interface for all assertion sources (assertions and continuations).
/// Extension methods target this interface, eliminating duplication.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public interface IAssertionSource<TValue> : IAssertionSource
{
    /// <summary>
    /// The assertion context shared by all assertions in this chain.
    /// Contains the evaluation context (value, timing, exceptions) and expression builder (error messages).
    /// </summary>
    AssertionContext<TValue> Context { get; }

    /// <summary>
    /// Asserts that the value is of the specified type and returns an assertion on the casted value.
    /// This allows chaining additional assertions on the typed value.
    /// Only available at assertion source points (initial Assert.That, or after .And/.Or).
    /// Example: await Assert.That(obj).IsTypeOf&lt;string&gt;().And.HasLength(5);
    /// </summary>
    TypeOfAssertion<TValue, TExpected> IsTypeOf<TExpected>();
}
