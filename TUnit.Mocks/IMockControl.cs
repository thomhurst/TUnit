using TUnit.Mocks.Diagnostics;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks;

/// <summary>
/// Strongly-typed framework operations for a <see cref="Mock{T}"/>.
/// Implemented explicitly on <see cref="Mock{T}"/> so the members never appear as instance candidates
/// during overload resolution — preventing them from shadowing source-generated extension methods
/// whose names happen to match. Reached via the static helpers on <see cref="Mock"/>.
/// </summary>
/// <remarks>
/// <see cref="IMock"/> carries the non-generic subset (<see cref="IMock.Reset"/>,
/// <see cref="IMock.VerifyAll"/>, <see cref="IMock.VerifyNoOtherCalls"/>, <c>ObjectInstance</c>)
/// because <see cref="MockRepository"/> needs to iterate mocks as a heterogeneous
/// <see cref="IMock"/>[] for batch operations. The typed members that depend on <c>T</c>
/// (<see cref="Invocations"/>, <see cref="GetDiagnostics"/>, the typed <see cref="Object"/>,
/// state-machine helpers) only make sense in the generic shape and live here.
/// </remarks>
internal interface IMockControl<T> : IMock where T : class
{
    /// <summary>The mock object that implements T.</summary>
    T Object { get; }

    /// <summary>All calls made to this mock, in order.</summary>
    IReadOnlyList<CallRecord> Invocations { get; }

    /// <summary>The mock behavior (Loose or Strict).</summary>
    MockBehavior Behavior { get; }

    /// <summary>
    /// Custom default-value provider consulted before auto-mocking and built-in defaults in loose mode.
    /// </summary>
    IDefaultValueProvider? DefaultValueProvider { get; set; }

    /// <summary>
    /// Enables auto-tracking for all properties. Setters store, getters return.
    /// Explicit setups take precedence.
    /// </summary>
    void SetupAllProperties();

    /// <summary>Returns a diagnostic report of setup coverage and call matching.</summary>
    MockDiagnostics GetDiagnostics();

    /// <summary>Sets the current state for state-machine mocking. Null clears.</summary>
    void SetState(string? stateName);

    /// <summary>
    /// Registers setups scoped to a specific state. Setups created inside <paramref name="configure"/>
    /// only match when the engine is in <paramref name="stateName"/>.
    /// </summary>
    void InState(string stateName, Action<Mock<T>> configure);
}
