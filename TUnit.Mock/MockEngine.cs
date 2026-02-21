using TUnit.Mock.Exceptions;
using TUnit.Mock.Setup;
using TUnit.Mock.Setup.Behaviors;
using TUnit.Mock.Verification;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace TUnit.Mock;

/// <summary>
/// Provides a globally unique sequence counter for ordering calls across all mock types.
/// </summary>
internal static class MockCallSequence
{
    private static long _counter;

    internal static long Next() => Interlocked.Increment(ref _counter);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MockEngine<T> where T : class
{
    private readonly List<MethodSetup> _setups = new();
    private readonly ReaderWriterLockSlim _setupLock = new();
    private readonly ConcurrentQueue<CallRecord> _callHistory = new();
    private readonly ConcurrentDictionary<string, object?> _autoTrackValues = new();
    private readonly ConcurrentQueue<(string EventName, bool IsSubscribe)> _eventSubscriptions = new();
    private readonly ConcurrentDictionary<string, Action> _onSubscribeCallbacks = new();
    private readonly ConcurrentDictionary<string, Action> _onUnsubscribeCallbacks = new();
    private readonly ConcurrentDictionary<string, IMock> _autoMockCache = new();

    /// <summary>
    /// When true, property setters automatically store values and getters return them,
    /// acting like real auto-properties. Explicit setups take precedence.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool AutoTrackProperties { get; set; }

    /// <summary>
    /// Reference to the mock impl as IRaisable, for auto-raising events after method calls.
    /// Set by the generated factory code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IRaisable? Raisable { get; set; }

    public MockBehavior Behavior { get; }

    public MockEngine(MockBehavior behavior)
    {
        Behavior = behavior;
        AutoTrackProperties = behavior == MockBehavior.Loose;
    }

    /// <summary>
    /// Registers a new setup. Thread-safe via writer lock.
    /// </summary>
    public void AddSetup(MethodSetup setup)
    {
        _setupLock.EnterWriteLock();
        try
        {
            _setups.Add(setup);
        }
        finally
        {
            _setupLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Handles a void method call. Records the call and executes matching setup behavior.
    /// </summary>
    public void HandleCall(int memberId, string memberName, object?[] args)
    {
        RecordCall(memberId, memberName, args);

        // Auto-track property setters: store value keyed by property name
        if (AutoTrackProperties && memberName.StartsWith("set_") && args.Length > 0)
        {
            _autoTrackValues[memberName.Substring(4)] = args[0];
        }

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, args);

        if (behavior is not null)
        {
            behavior.Execute(args);
            if (matchedSetup is not null) RaiseEventsForSetup(matchedSetup);
            return;
        }

        // A matching setup with no explicit behavior means "allow this call" (e.g., void setup with no callback)
        if (setupFound)
        {
            if (matchedSetup is not null) RaiseEventsForSetup(matchedSetup);
            return;
        }

        if (Behavior == MockBehavior.Strict)
        {
            var callDesc = FormatCall(memberName, args);
            throw new MockStrictBehaviorException(callDesc);
        }
    }

    /// <summary>
    /// Handles a method call with a return value. Records the call, executes matching setup,
    /// or returns default/throws for strict mode.
    /// </summary>
    public TReturn HandleCallWithReturn<TReturn>(int memberId, string memberName, object?[] args, TReturn defaultValue)
    {
        RecordCall(memberId, memberName, args);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, args);

        if (behavior is not null)
        {
            var result = behavior.Execute(args);
            if (matchedSetup is not null) RaiseEventsForSetup(matchedSetup);
            if (result is TReturn typed) return typed;
            if (result is null) return default(TReturn)!;
            return defaultValue;
        }

        // A matching setup with no explicit behavior returns the default value
        if (setupFound)
        {
            if (matchedSetup is not null) RaiseEventsForSetup(matchedSetup);
            return defaultValue;
        }

        // Auto-track property getters: return stored value if available
        if (AutoTrackProperties && memberName.StartsWith("get_"))
        {
            if (_autoTrackValues.TryGetValue(memberName.Substring(4), out var trackedValue))
            {
                if (trackedValue is TReturn typed) return typed;
                if (trackedValue is null) return default(TReturn)!;
            }
        }

        // Auto-mock: for interface return types in Loose mode, create a functional mock
        if (Behavior == MockBehavior.Loose && typeof(TReturn).IsInterface)
        {
            var cacheKey = memberName + "|" + typeof(TReturn).FullName;
            if (_autoMockCache.TryGetValue(cacheKey, out var cached))
            {
                return (TReturn)cached.ObjectInstance;
            }

            if (Mock.TryCreateAutoMock(typeof(TReturn), Behavior, out var autoMock))
            {
                _autoMockCache[cacheKey] = autoMock;
                return (TReturn)autoMock.ObjectInstance;
            }
        }

        if (Behavior == MockBehavior.Strict)
        {
            var callDesc = FormatCall(memberName, args);
            throw new MockStrictBehaviorException(callDesc);
        }

        return defaultValue;
    }

    /// <summary>
    /// Handles a void method call for a partial mock virtual method.
    /// Records the call, executes matching setup behavior if found.
    /// Returns true if a setup was found (caller should NOT call base), false otherwise (caller should call base).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCall(int memberId, string memberName, object?[] args)
    {
        RecordCall(memberId, memberName, args);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, args);

        if (behavior is not null)
        {
            behavior.Execute(args);
            if (matchedSetup is not null) RaiseEventsForSetup(matchedSetup);
            return true;
        }

        if (setupFound && matchedSetup is not null)
        {
            RaiseEventsForSetup(matchedSetup);
        }

        return setupFound;
    }

    /// <summary>
    /// Handles a method call with a return value for a partial mock virtual method.
    /// Records the call, executes matching setup if found.
    /// Returns true if a setup was found (result is set), false otherwise (caller should call base).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn>(int memberId, string memberName, object?[] args, TReturn defaultValue, out TReturn result)
    {
        RecordCall(memberId, memberName, args);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, args);

        if (behavior is not null)
        {
            var behaviorResult = behavior.Execute(args);
            if (matchedSetup is not null) RaiseEventsForSetup(matchedSetup);
            if (behaviorResult is TReturn typed) result = typed;
            else if (behaviorResult is null) result = default(TReturn)!;
            else result = defaultValue;
            return true;
        }

        if (setupFound)
        {
            if (matchedSetup is not null) RaiseEventsForSetup(matchedSetup);
            result = defaultValue;
            return true;
        }

        result = defaultValue;
        return false;
    }

    /// <summary>
    /// Gets all recorded calls for a specific member, optionally filtered by matchers.
    /// </summary>
    public IReadOnlyList<CallRecord> GetCallsFor(int memberId)
    {
        var result = new List<CallRecord>();
        foreach (var record in _callHistory)
        {
            if (record.MemberId == memberId)
            {
                result.Add(record);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets all recorded calls.
    /// </summary>
    public IReadOnlyList<CallRecord> GetAllCalls()
    {
        return _callHistory.ToArray();
    }

    /// <summary>
    /// Gets all recorded calls that have not been matched by a verification statement.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IReadOnlyList<CallRecord> GetUnverifiedCalls()
    {
        var result = new List<CallRecord>();
        foreach (var record in _callHistory)
        {
            if (!record.IsVerified)
            {
                result.Add(record);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets all registered setups.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IReadOnlyList<MethodSetup> GetSetups()
    {
        _setupLock.EnterReadLock();
        try
        {
            return _setups.ToList();
        }
        finally
        {
            _setupLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Clears all setups and call history.
    /// </summary>
    /// <summary>
    /// Tries to get a cached auto-mock by its cache key. Used by Mock&lt;T&gt;.GetAutoMock.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryGetAutoMock(string cacheKey, out IMock mock)
    {
        return _autoMockCache.TryGetValue(cacheKey, out mock!);
    }

    public void Reset()
    {
        _setupLock.EnterWriteLock();
        try
        {
            _setups.Clear();
        }
        finally
        {
            _setupLock.ExitWriteLock();
        }

        // Drain the queue
        while (_callHistory.TryDequeue(out _)) { }

        _autoTrackValues.Clear();
        while (_eventSubscriptions.TryDequeue(out _)) { }
        _onSubscribeCallbacks.Clear();
        _onUnsubscribeCallbacks.Clear();
        _autoMockCache.Clear();
    }

    /// <summary>
    /// Registers a callback to invoke when an event is subscribed to.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void OnSubscribe(string eventName, Action callback)
    {
        _onSubscribeCallbacks[eventName] = callback;
    }

    /// <summary>
    /// Registers a callback to invoke when an event is unsubscribed from.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void OnUnsubscribe(string eventName, Action callback)
    {
        _onUnsubscribeCallbacks[eventName] = callback;
    }

    /// <summary>
    /// Records an event subscription (add) or unsubscription (remove). Called by generated event accessors.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RecordEventSubscription(string eventName, bool isSubscribe)
    {
        _eventSubscriptions.Enqueue((eventName, isSubscribe));

        if (isSubscribe)
        {
            if (_onSubscribeCallbacks.TryGetValue(eventName, out var callback))
            {
                callback();
            }
        }
        else
        {
            if (_onUnsubscribeCallbacks.TryGetValue(eventName, out var callback))
            {
                callback();
            }
        }
    }

    /// <summary>
    /// Gets the current subscriber count for a named event (subscribes minus unsubscribes).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public int GetEventSubscriberCount(string eventName)
    {
        int count = 0;
        foreach (var (name, isSub) in _eventSubscriptions)
        {
            if (name == eventName)
            {
                count += isSub ? 1 : -1;
            }
        }
        return Math.Max(0, count);
    }

    /// <summary>
    /// Returns true if any subscription was ever recorded for the named event.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool WasEventSubscribed(string eventName)
    {
        foreach (var (name, isSub) in _eventSubscriptions)
        {
            if (name == eventName && isSub) return true;
        }
        return false;
    }

    private void RecordCall(int memberId, string memberName, object?[] args)
    {
        var seq = MockCallSequence.Next();
        _callHistory.Enqueue(new CallRecord(memberId, memberName, args, DateTime.UtcNow, seq));
    }

    private void RaiseEventsForSetup(MethodSetup setup)
    {
        if (Raisable is null) return;

        var raises = setup.GetEventRaises();
        foreach (var raise in raises)
        {
            Raisable.RaiseEvent(raise.EventName, raise.Args);
        }
    }

    private (bool SetupFound, IBehavior? Behavior, MethodSetup? Setup) FindMatchingSetup(int memberId, object?[] args)
    {
        _setupLock.EnterReadLock();
        try
        {
            // Iterate last-added-first to implement "last wins" semantics
            for (int i = _setups.Count - 1; i >= 0; i--)
            {
                var setup = _setups[i];
                if (setup.MemberId == memberId && setup.Matches(args))
                {
                    setup.IncrementInvokeCount();
                    return (true, setup.GetNextBehavior(), setup);
                }
            }
        }
        finally
        {
            _setupLock.ExitReadLock();
        }

        return (false, null, null);
    }

    private static string FormatCall(string memberName, object?[] args)
    {
        var formattedArgs = string.Join(", ", args.Select(a => a?.ToString() ?? "null"));
        return $"{memberName}({formattedArgs})";
    }
}
