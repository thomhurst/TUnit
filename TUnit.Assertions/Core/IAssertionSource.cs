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
    /// Asserts that the value is assignment-compatible with the specified type.
    /// </summary>
    TypeOfAssertion<TValue, TExpected> IsTypeOf<TExpected>();

    /// <summary>
    /// Asserts that the value is NOT exactly of the specified type.
    /// </summary>
    IsNotTypeOfAssertion<TValue, TExpected> IsNotTypeOf<TExpected>();

    /// <summary>
    /// Asserts that the value's type is assignable to the specified type.
    /// </summary>
    IsAssignableToAssertion<TExpected, TValue> IsAssignableTo<TExpected>();

    /// <summary>
    /// Asserts that the value's type is NOT assignable to the specified type.
    /// </summary>
    IsNotAssignableToAssertion<TExpected, TValue> IsNotAssignableTo<TExpected>();

    /// <summary>
    /// Asserts that the value's type is assignable from the specified type.
    /// </summary>
    IsAssignableFromAssertion<TExpected, TValue> IsAssignableFrom<TExpected>();

    /// <summary>
    /// Asserts that the value's type is NOT assignable from the specified type.
    /// </summary>
    IsNotAssignableFromAssertion<TExpected, TValue> IsNotAssignableFrom<TExpected>();
}
