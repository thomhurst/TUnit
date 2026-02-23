using System.ComponentModel;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks;

/// <summary>
/// Wraps a generated mock implementation. Source-generated extension methods on <c>Mock&lt;T&gt;</c>
/// provide strongly-typed setup and verification members directly.
/// </summary>
public class Mock<T> : IMock where T : class
{
    /// <summary>The mock engine. Used by generated code. Not intended for direct use.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public MockEngine<T> Engine { get; }

    /// <summary>The mock object that implements T.</summary>
    public T Object { get; }

    /// <inheritdoc />
    object IMock.ObjectInstance => Object;

    /// <summary>The mock behavior (Loose or Strict).</summary>
    public MockBehavior Behavior => Engine.Behavior;

    /// <summary>
    /// Gets or sets the custom default value provider for unconfigured methods in loose mode.
    /// When set, this provider is consulted before auto-mocking and built-in defaults.
    /// </summary>
    public IDefaultValueProvider? DefaultValueProvider
    {
        get => Engine.DefaultValueProvider;
        set => Engine.DefaultValueProvider = value;
    }

    /// <summary>Creates a Mock wrapping the given object and engine. Used by generated code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Mock(T mockObject, MockEngine<T> engine)
    {
        Engine = engine;
        Object = mockObject;
    }

    /// <summary>All calls made to this mock, in order.</summary>
    public IReadOnlyList<CallRecord> Invocations => Engine.GetAllCalls();

    /// <summary>
    /// Enables auto-tracking for all properties. Property setters store values and getters return them,
    /// acting like real auto-properties. Explicit setups take precedence over auto-tracked values.
    /// </summary>
    public void SetupAllProperties()
    {
        Engine.AutoTrackProperties = true;
    }

    /// <summary>Clears all setups and call history.</summary>
    public void Reset() => Engine.Reset();

    /// <summary>
    /// Verifies all registered setups were invoked at least once.
    /// Throws <see cref="MockVerificationException"/> listing uninvoked setups.
    /// </summary>
    public void VerifyAll()
    {
        var setups = Engine.GetSetups();
        var uninvoked = new List<string>();
        foreach (var setup in setups)
        {
            if (setup.InvokeCount == 0)
            {
                var matchers = setup.GetMatcherDescriptions();
                var desc = matchers.Length > 0
                    ? $"{setup.MemberName}({string.Join(", ", matchers)})"
                    : $"{setup.MemberName}()";
                uninvoked.Add(desc);
            }
        }

        if (uninvoked.Count > 0)
        {
            var message = $"VerifyAll failed. The following setups were never invoked:\n" +
                          string.Join("\n", uninvoked.Select(s => $"  - {s}"));
            throw new MockVerificationException(message);
        }
    }

    /// <summary>
    /// Fails if any recorded call was not matched by a prior verification statement.
    /// Throws <see cref="MockVerificationException"/> listing unverified calls.
    /// </summary>
    public void VerifyNoOtherCalls()
    {
        var unverified = Engine.GetUnverifiedCalls();
        if (unverified.Count > 0)
        {
            var message = $"VerifyNoOtherCalls failed. The following calls were not verified:\n" +
                          string.Join("\n", unverified.Select(c => $"  - {c.FormatCall()}"));
            throw new MockVerificationException(message);
        }
    }

    /// <summary>
    /// Retrieves the auto-mock wrapper for a child interface returned by this mock.
    /// Use this to configure auto-mocked return values.
    /// </summary>
    public Mock<TChild> GetAutoMock<TChild>(string memberName) where TChild : class
    {
        var cacheKey = memberName + "|" + typeof(TChild).FullName;
        if (Engine.TryGetAutoMock(cacheKey, out var mock))
        {
            return (Mock<TChild>)mock;
        }

        throw new InvalidOperationException(
            $"No auto-mock found for member '{memberName}' returning type '{typeof(TChild).Name}'. " +
            $"Ensure the method was called at least once before retrieving its auto-mock.");
    }

    /// <summary>
    /// Returns a diagnostic report of this mock's setup coverage and call matching.
    /// </summary>
    public Diagnostics.MockDiagnostics GetDiagnostics() => Engine.GetDiagnostics();

    /// <summary>
    /// Sets the current state for state machine mocking. Null clears the state.
    /// </summary>
    public void SetState(string? stateName) => Engine.TransitionTo(stateName);

    /// <summary>
    /// Configures setups scoped to a specific state. All setups registered inside
    /// the <paramref name="configure"/> action will only match when the engine is in the specified state.
    /// </summary>
    public void InState(string stateName, Action<Mock<T>> configure)
    {
        var previous = Engine.PendingRequiredState;
        Engine.PendingRequiredState = stateName;
        try
        {
            configure(this);
        }
        finally
        {
            Engine.PendingRequiredState = previous;
        }
    }

    /// <summary>Implicit conversion to T -- no .Object needed.</summary>
    public static implicit operator T(Mock<T> mock) => mock.Object;
}
