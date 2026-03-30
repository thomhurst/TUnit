using System.ComponentModel;
using TUnit.Mocks.Diagnostics;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks;

/// <summary>
/// Wraps a generated mock implementation. Source-generated extension methods on <c>Mock&lt;T&gt;</c>
/// provide strongly-typed setup and verification members directly.
/// </summary>
public class Mock<T> : IMock, IMockEngineAccess<T> where T : class
{
    private readonly MockEngine<T> _engine;

    /// <summary>The mock object that implements T.</summary>
    public T Object { get; }

    /// <inheritdoc />
    object IMock.ObjectInstance => Object;

    /// <inheritdoc />
    MockEngine<T> IMockEngineAccess<T>.Engine => _engine;

    /// <summary>Creates a Mock wrapping the given object and engine. Used by generated code.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Mock(T mockObject, MockEngine<T> engine)
    {
        _engine = engine;
        Object = mockObject;
        if (mockObject is IMockObject mockObj)
        {
            mockObj.MockWrapper = this;
        }
    }

    /// <summary>All calls made to this mock, in order.</summary>
    public IReadOnlyList<CallRecord> Invocations => _engine.GetAllCalls();

    /// <summary>Returns the mock behavior (Loose or Strict).</summary>
    public MockBehavior Behavior => _engine.Behavior;

    /// <summary>
    /// Gets or sets the custom default value provider for unconfigured methods in loose mode.
    /// When set, this provider is consulted before auto-mocking and built-in defaults.
    /// </summary>
    public IDefaultValueProvider? DefaultValueProvider
    {
        get => _engine.DefaultValueProvider;
        set => _engine.DefaultValueProvider = value;
    }

    /// <summary>
    /// Enables auto-tracking for all properties. Property setters store values and getters return them,
    /// acting like real auto-properties. Explicit setups take precedence over auto-tracked values.
    /// </summary>
    public void SetupAllProperties() => _engine.AutoTrackProperties = true;

    /// <summary>Clears all setups and call history.</summary>
    public void Reset() => _engine.Reset();

    /// <summary>
    /// Verifies all registered setups were invoked at least once.
    /// Throws <see cref="MockVerificationException"/> listing uninvoked setups.
    /// </summary>
    public void VerifyAll()
    {
        var setups = _engine.GetSetups();
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
        var unverified = _engine.GetUnverifiedCalls();
        if (unverified.Count > 0)
        {
            var message = $"VerifyNoOtherCalls failed. The following calls were not verified:\n" +
                          string.Join("\n", unverified.Select(c => $"  - {c.FormatCall()}"));
            throw new MockVerificationException(message);
        }
    }

    /// <summary>
    /// Returns a diagnostic report of this mock's setup coverage and call matching.
    /// </summary>
    public MockDiagnostics GetDiagnostics() => _engine.GetDiagnostics();

    /// <summary>
    /// Sets the current state for state machine mocking. Null clears the state.
    /// </summary>
    public void SetState(string? stateName) => _engine.TransitionTo(stateName);

    /// <summary>
    /// Configures setups scoped to a specific state. All setups registered inside
    /// the <paramref name="configure"/> action will only match when the engine is in the specified state.
    /// </summary>
    public void InState(string stateName, Action<Mock<T>> configure)
    {
        var previous = _engine.PendingRequiredState;
        _engine.PendingRequiredState = stateName;
        try
        {
            configure(this);
        }
        finally
        {
            _engine.PendingRequiredState = previous;
        }
    }

    /// <inheritdoc />
    void IMock.VerifyAll() => VerifyAll();

    /// <inheritdoc />
    void IMock.VerifyNoOtherCalls() => VerifyNoOtherCalls();

    /// <inheritdoc />
    void IMock.Reset() => Reset();

    /// <summary>Implicit conversion to T -- no .Object needed.</summary>
    public static implicit operator T(Mock<T> mock) => mock.Object;
}
