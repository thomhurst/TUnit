using System.ComponentModel;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Setup;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks;

/// <summary>
/// Non-generic interface for interacting with a mock engine.
/// Used by property accessor structs that don't know the mock's type parameter.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMockEngineAccess
{
    /// <summary>Registers a new method setup with the engine.</summary>
    void AddSetup(MethodSetup setup);

    /// <summary>Creates a call verification builder for the specified member.</summary>
    ICallVerification CreateVerification(int memberId, string memberName, IArgumentMatcher[] matchers);

    /// <summary>Registers a callback that fires when a handler subscribes to the named event.</summary>
    void OnSubscribe(string eventName, Action callback);

    /// <summary>Registers a callback that fires when a handler unsubscribes from the named event.</summary>
    void OnUnsubscribe(string eventName, Action callback);

    /// <summary>Gets the current subscriber count for the named event.</summary>
    int GetEventSubscriberCount(string eventName);

    /// <summary>Returns true if the named event was ever subscribed to.</summary>
    bool WasEventSubscribed(string eventName);
}
