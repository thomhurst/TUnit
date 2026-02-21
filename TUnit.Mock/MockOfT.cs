using TUnit.Mock.Exceptions;
using TUnit.Mock.Verification;

namespace TUnit.Mock;

/// <summary>
/// Wraps a generated mock implementation and provides access to Setup, Verify, and Raise surfaces.
/// <para>
/// The source generator produces extension methods on <c>IMockSetup&lt;T&gt;</c>, <c>IMockVerify&lt;T&gt;</c>,
/// and <c>IMockRaise&lt;T&gt;</c> that provide strongly-typed setup/verify/raise members.
/// This approach is fully AOT-compatible — no dynamic dispatch is needed.
/// </para>
/// </summary>
public class Mock<T> : IMock where T : class
{
    /// <summary>The mock engine. Used by generated code. Not intended for direct use.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public MockEngine<T> Engine { get; }

    /// <summary>The mock object that implements T.</summary>
    public T Object { get; }

    /// <inheritdoc />
    object IMock.ObjectInstance => Object;

    /// <summary>The mock behavior (Loose or Strict).</summary>
    public MockBehavior Behavior => Engine.Behavior;

    /// <summary>
    /// Generated setup surface -- extension methods provide T's members with <c>Arg{T}</c> parameters.
    /// </summary>
    public IMockSetup<T> Setup { get; }

    /// <summary>
    /// Generated verification surface -- extension methods provide T's members with <c>Arg{T}</c> parameters.
    /// </summary>
    public IMockVerify<T>? Verify { get; internal set; }

    /// <summary>
    /// Generated event-raising surface (if T has events).
    /// </summary>
    public IMockRaise<T>? Raise { get; internal set; }

    /// <summary>Creates a Mock with a new engine. Used by generated code.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Mock(T mockObject, object setup, MockBehavior behavior)
    {
        Engine = new MockEngine<T>(behavior);
        Object = mockObject;
        Setup = (IMockSetup<T>)setup;
    }

    /// <summary>Creates a Mock with an existing engine. Used by generated code.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Mock(T mockObject, object setup, MockEngine<T> engine)
    {
        Engine = engine;
        Object = mockObject;
        Setup = (IMockSetup<T>)setup;
    }

    /// <summary>Creates a Mock with an existing engine, setup, and verify surfaces. Used by generated code.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Mock(T mockObject, object setup, object? verify, MockEngine<T> engine)
    {
        Engine = engine;
        Object = mockObject;
        Setup = (IMockSetup<T>)setup;
        Verify = (IMockVerify<T>?)verify;
    }

    /// <summary>Creates a Mock with an existing engine, setup, verify, and raise surfaces. Used by generated code.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Mock(T mockObject, object setup, object? verify, object? raise, MockEngine<T> engine)
    {
        Engine = engine;
        Object = mockObject;
        Setup = (IMockSetup<T>)setup;
        Verify = (IMockVerify<T>?)verify;
        Raise = (IMockRaise<T>?)raise;
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
    /// Registers a callback that fires when a handler subscribes to the named event.
    /// </summary>
    public void OnSubscribe(string eventName, Action callback) => Engine.OnSubscribe(eventName, callback);

    /// <summary>
    /// Registers a callback that fires when a handler unsubscribes from the named event.
    /// </summary>
    public void OnUnsubscribe(string eventName, Action callback) => Engine.OnUnsubscribe(eventName, callback);

    /// <summary>
    /// Gets the current number of subscribers for the named event.
    /// </summary>
    public int GetEventSubscriberCount(string eventName) => Engine.GetEventSubscriberCount(eventName);

    /// <summary>
    /// Returns true if the named event was ever subscribed to.
    /// </summary>
    public bool WasEventSubscribed(string eventName) => Engine.WasEventSubscribed(eventName);

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
                uninvoked.Add(setup.MemberName);
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

    /// <summary>Implicit conversion to T -- no .Object needed.</summary>
    public static implicit operator T(Mock<T> mock) => mock.Object;
}
