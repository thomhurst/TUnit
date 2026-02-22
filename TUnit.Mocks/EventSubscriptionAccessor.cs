using System.ComponentModel;

namespace TUnit.Mocks;

/// <summary>
/// Lightweight accessor for event subscription queries and callbacks.
/// Returns from generated extension properties like <c>mock.Events.StatusChanged</c>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct EventSubscriptionAccessor
{
    private readonly IMockEngineAccess _engine;
    private readonly string _eventName;

    /// <summary>Creates a new event subscription accessor.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public EventSubscriptionAccessor(IMockEngineAccess engine, string eventName)
    {
        _engine = engine;
        _eventName = eventName;
    }

    /// <summary>Returns true if the event was ever subscribed to.</summary>
    public bool WasSubscribed => _engine.WasEventSubscribed(_eventName);

    /// <summary>Gets the current number of subscribers for this event.</summary>
    public int SubscriberCount => _engine.GetEventSubscriberCount(_eventName);

    /// <summary>Registers a callback that fires when a handler subscribes to this event.</summary>
    public void OnSubscribe(Action callback) => _engine.OnSubscribe(_eventName, callback);

    /// <summary>Registers a callback that fires when a handler unsubscribes from this event.</summary>
    public void OnUnsubscribe(Action callback) => _engine.OnUnsubscribe(_eventName, callback);
}
