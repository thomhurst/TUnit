using TUnit.Mocks.Arguments;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Setup;
using TUnit.Mocks.Setup.Behaviors;
using TUnit.Mocks.Verification;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.ComponentModel;

namespace TUnit.Mocks;

/// <summary>
/// Provides a globally unique sequence counter for ordering calls across all mock types.
/// </summary>
internal static class MockCallSequence
{
    private static long _counter;

    internal static long Next() => Interlocked.Increment(ref _counter);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MockEngine<T> : IMockEngineAccess where T : class
{
    // Single lock for both setup and call mutations — reduces allocation by one Lock object.
    // Contention is acceptable since setup and call recording rarely overlap in typical usage.
    private Lock? _lock;
    private Lock Lock => _lock ?? EnsureLock();

    // Flat array indexed by memberId for O(1) setup lookup (member IDs are dense sequential ints 0..N)
    private volatile MethodSetup[]?[]? _setupsByMemberId;
    private List<MethodSetup>?[]? _setupListsByMemberId; // mutable lists used during AddSetup, guarded by Lock
    private volatile bool _hasStatefulSetups;
    private volatile bool _hasStaleSetups; // true when setup lists have been modified but snapshots not yet rebuilt

    // Call arrays are pre-allocated during setup to avoid capacity checks on the invocation hot path.
    private List<CallRecord>?[]? _callsByMemberId;
    private volatile int[]? _callCountByMemberId;

    private ConcurrentDictionary<string, object?>? _autoTrackValues;
    private ConcurrentQueue<(string EventName, bool IsSubscribe)>? _eventSubscriptions;
    private ConcurrentDictionary<string, Action>? _onSubscribeCallbacks;
    private ConcurrentDictionary<string, Action>? _onUnsubscribeCallbacks;
    private ConcurrentDictionary<string, IMock?>? _autoMockCache;

    /// <summary>
    /// The current state name for state machine mocking. Null means no state (all setups match).
    /// </summary>
    private string? _currentState;

    /// <summary>
    /// Temporary state set during <see cref="Mock{T}.InState"/> scope.
    /// When non-null, <see cref="AddSetup"/> stamps this onto new setups' <see cref="MethodSetup.RequiredState"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string? PendingRequiredState { get; set; }

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

    /// <summary>
    /// When true, indicates this engine backs a wrap mock. In Strict mode, unconfigured calls
    /// throw instead of falling through to the wrapped instance. Set by generated factory code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsWrapMock { get; set; }

    /// <summary>
    /// Gets or sets a custom default value provider for unconfigured method return types in loose mode.
    /// When set, this provider is consulted before auto-mocking and built-in defaults.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IDefaultValueProvider? DefaultValueProvider { get; set; }

    public MockBehavior Behavior { get; }

    public MockEngine(MockBehavior behavior)
    {
        Behavior = behavior;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Lock EnsureLock()
    {
        Interlocked.CompareExchange(ref _lock, new Lock(), null);
        return _lock!;
    }

    private ConcurrentDictionary<string, object?> AutoTrackValues
        => LazyInitializer.EnsureInitialized(ref _autoTrackValues)!;

    private ConcurrentQueue<(string EventName, bool IsSubscribe)> EventSubscriptions
        => LazyInitializer.EnsureInitialized(ref _eventSubscriptions)!;

    private ConcurrentDictionary<string, Action> OnSubscribeCallbacks
        => LazyInitializer.EnsureInitialized(ref _onSubscribeCallbacks)!;

    private ConcurrentDictionary<string, Action> OnUnsubscribeCallbacks
        => LazyInitializer.EnsureInitialized(ref _onUnsubscribeCallbacks)!;

    private ConcurrentDictionary<string, IMock?> AutoMockCache
        => LazyInitializer.EnsureInitialized(ref _autoMockCache)!;

    /// <summary>
    /// Transitions the engine to the specified state. Null clears the state.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void TransitionTo(string? stateName) { lock (Lock) { _currentState = stateName; } }

    /// <summary>
    /// Gets the current state name. Null means no state.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string? CurrentState { get { lock (Lock) { return _currentState; } } }

    /// <summary>
    /// Registers a new setup. Thread-safe via lock.
    /// If <see cref="PendingRequiredState"/> is set, stamps it onto the setup.
    /// </summary>
    public void AddSetup(MethodSetup setup)
    {
        lock (Lock)
        {
            if (PendingRequiredState is not null)
            {
                setup.RequiredState = PendingRequiredState;
            }

            var memberId = setup.MemberId;
            EnsureSetupArrayCapacity(memberId);

            var lists = _setupListsByMemberId!;
            var list = lists[memberId] ??= new();
            list.Add(setup);

            if (setup.RequiredState is not null || setup.TransitionTarget is not null)
            {
                _hasStatefulSetups = true;
            }

            // Mark stale — snapshot will be rebuilt lazily on next FindMatchingSetup call.
            // This avoids repeated ToArray allocations during the setup phase.
            _hasStaleSetups = true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureSetupArrayCapacity(int memberId)
    {
        var required = memberId + 1;
        if (_setupsByMemberId is null || _setupsByMemberId.Length < required)
        {
            var newSize = _setupsByMemberId is null
                ? Math.Max(required, 8)
                : Math.Max(required, _setupsByMemberId.Length * 2);
            var newSnapshot = new MethodSetup[]?[newSize];
            var newLists = new List<MethodSetup>?[newSize];
            if (_setupsByMemberId is not null)
            {
                Array.Copy(_setupsByMemberId, newSnapshot, _setupsByMemberId.Length);
                Array.Copy(_setupListsByMemberId!, newLists, _setupListsByMemberId!.Length);
            }
            _setupsByMemberId = newSnapshot;
            _setupListsByMemberId = newLists;
        }

        // Pre-allocate call tracking arrays so invocations don't need to check capacity
        EnsureCallArrayCapacity(memberId);
    }

    /// <inheritdoc />
    ICallVerification IMockEngineAccess.CreateVerification(int memberId, string memberName, IArgumentMatcher[] matchers)
        => new CallVerificationBuilder<T>(this, memberId, memberName, matchers);

    /// <summary>
    /// Handles a void method call. Records the call and executes matching setup behavior.
    /// </summary>
    public void HandleCall(int memberId, string memberName, object?[] args)
    {
        RawReturnContext.Clear();
        var callRecord = RecordCall(memberId, memberName, args);

        // Auto-track property setters: store value keyed by property name
        if (AutoTrackProperties && memberName.StartsWith("set_", StringComparison.Ordinal) && args.Length > 0)
        {
            AutoTrackValues[memberName[4..]] = args[0];
        }

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, args);

        if (behavior is not null)
        {
            var behaviorResult = behavior.Execute(args);
            if (behaviorResult is RawReturn raw)
            {
                RawReturnContext.Set(raw);
            }
            try
            {
                ApplyMatchedSetup(matchedSetup);
            }
            catch
            {
                RawReturnContext.Clear();
                throw;
            }
            return;
        }

        ApplyMatchedSetup(matchedSetup);

        // A matching setup with no explicit behavior means "allow this call" (e.g., void setup with no callback)
        if (setupFound)
        {
            return;
        }

        // No setup matched — mark as unmatched for diagnostics
        callRecord.IsUnmatched = true;

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
        RawReturnContext.Clear();
        var callRecord = RecordCall(memberId, memberName, args);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, args);

        if (behavior is not null)
        {
            var result = behavior.Execute(args);
            if (result is RawReturn raw)
            {
                RawReturnContext.Set(raw);
            }
            try
            {
                ApplyMatchedSetup(matchedSetup);
            }
            catch
            {
                RawReturnContext.Clear();
                throw;
            }
            if (result is TReturn typed) return typed;
            if (result is null) return default(TReturn)!;
            if (result is RawReturn)
            {
                return defaultValue;
            }
            throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {result.GetType().Name}.");
        }

        ApplyMatchedSetup(matchedSetup);

        // A matching setup with no explicit behavior returns the default value
        if (setupFound)
        {
            return defaultValue;
        }

        // No setup matched — mark as unmatched for diagnostics
        callRecord.IsUnmatched = true;

        // Auto-track property getters: return stored value if available
        if (AutoTrackProperties && Volatile.Read(ref _autoTrackValues) is { } trackValues && memberName.StartsWith("get_", StringComparison.Ordinal))
        {
            if (trackValues.TryGetValue(memberName[4..], out var trackedValue))
            {
                if (trackedValue is TReturn typed) return typed;
                if (trackedValue is null) return default(TReturn)!;
            }
        }

        // Custom default value provider: consulted before auto-mock and built-in defaults.
        // Suppressed: DefaultValueProvider is opt-in (null by default). Users who set it accept the AOT tradeoff.
        // The source generator emits inline defaults for Task<T>/ValueTask<T>/collections without needing this path.
#pragma warning disable IL3050, IL2026
        if (DefaultValueProvider is not null && DefaultValueProvider.CanProvide(typeof(TReturn)))
        {
            var customDefault = DefaultValueProvider.GetDefaultValue(typeof(TReturn));
            if (customDefault is TReturn typedCustom) return typedCustom;
            if (customDefault is null) return default(TReturn)!;
        }
#pragma warning restore IL3050, IL2026

        // Auto-mock: for interface return types in Loose mode, create a functional mock
        if (Behavior == MockBehavior.Loose && typeof(TReturn).IsInterface)
        {
            var cacheKey = memberName + "|" + typeof(TReturn).FullName;
            var autoMock = AutoMockCache.GetOrAdd(cacheKey, _ =>
            {
                MockRegistry.TryCreateAutoMock(typeof(TReturn), Behavior, out var m);
                return m;
            });
            if (autoMock is not null)
            {
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
    /// Handles a void method call for a partial/wrap mock virtual method.
    /// Records the call, executes matching setup behavior if found.
    /// Returns true if a setup was found (caller should NOT call base), false otherwise (caller should call base).
    /// In Strict mode, throws if no setup matches (no fallthrough to base).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCall(int memberId, string memberName, object?[] args)
    {
        RawReturnContext.Clear();
        var callRecord = RecordCall(memberId, memberName, args);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, args);

        if (behavior is not null)
        {
            var behaviorResult = behavior.Execute(args);
            if (behaviorResult is RawReturn raw)
            {
                RawReturnContext.Set(raw);
            }
            try
            {
                ApplyMatchedSetup(matchedSetup);
            }
            catch
            {
                RawReturnContext.Clear();
                throw;
            }
            return true;
        }

        ApplyMatchedSetup(matchedSetup);

        if (!setupFound)
        {
            callRecord.IsUnmatched = true;
        }

        if (!setupFound && IsWrapMock && Behavior == MockBehavior.Strict)
        {
            var callDesc = FormatCall(memberName, args);
            throw new MockStrictBehaviorException(callDesc);
        }

        return setupFound;
    }

    /// <summary>
    /// Handles a method call with a return value for a partial/wrap mock virtual method.
    /// Records the call, executes matching setup if found.
    /// Returns true if a setup was found (result is set), false otherwise (caller should call base).
    /// In Strict mode, throws if no setup matches (no fallthrough to base).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryHandleCallWithReturn<TReturn>(int memberId, string memberName, object?[] args, TReturn defaultValue, out TReturn result)
    {
        RawReturnContext.Clear();
        var callRecord = RecordCall(memberId, memberName, args);

        var (setupFound, behavior, matchedSetup) = FindMatchingSetup(memberId, args);

        if (behavior is not null)
        {
            var behaviorResult = behavior.Execute(args);
            if (behaviorResult is RawReturn raw)
            {
                RawReturnContext.Set(raw);
            }
            try
            {
                ApplyMatchedSetup(matchedSetup);
            }
            catch
            {
                RawReturnContext.Clear();
                throw;
            }
            if (behaviorResult is TReturn typed) result = typed;
            else if (behaviorResult is null) result = default(TReturn)!;
            else if (behaviorResult is RawReturn)
            {
                result = defaultValue;
            }
            else throw new InvalidOperationException(
                $"Setup for method returning {typeof(TReturn).Name} returned incompatible type {behaviorResult.GetType().Name}.");
            return true;
        }

        ApplyMatchedSetup(matchedSetup);

        if (setupFound)
        {
            result = defaultValue;
            return true;
        }

        // No setup matched
        callRecord.IsUnmatched = true;

        if (IsWrapMock && Behavior == MockBehavior.Strict)
        {
            var callDesc = FormatCall(memberName, args);
            throw new MockStrictBehaviorException(callDesc);
        }

        result = defaultValue;
        return false;
    }

    /// <summary>
    /// Gets all recorded calls for a specific member, optionally filtered by matchers.
    /// </summary>
    public IReadOnlyList<CallRecord> GetCallsFor(int memberId)
    {
        lock (Lock)
        {
            if (_callsByMemberId is not null && (uint)memberId < (uint)_callsByMemberId.Length
                && _callsByMemberId[memberId] is { } list)
            {
                return list.ToArray();
            }
        }
        return [];
    }

    /// <summary>
    /// Marks all calls for a specific member as verified without allocating a copy.
    /// </summary>
    internal void MarkCallsVerified(int memberId)
    {
        lock (Lock)
        {
            if (_callsByMemberId is not null && (uint)memberId < (uint)_callsByMemberId.Length
                && _callsByMemberId[memberId] is { } list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].IsVerified = true;
                }
            }
        }
    }

    /// <summary>
    /// Gets the count of calls for a specific member without allocating.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public int GetCallCountFor(int memberId)
    {
        var counts = _callCountByMemberId; // volatile read
        if (counts is not null && (uint)memberId < (uint)counts.Length)
        {
            return Volatile.Read(ref counts[memberId]);
        }
        return 0;
    }

    /// <summary>
    /// Gets all recorded calls.
    /// </summary>
    public IReadOnlyList<CallRecord> GetAllCalls()
    {
        lock (Lock)
        {
            if (_callsByMemberId is null) return [];
            var all = new List<CallRecord>();
            CollectCallRecords(all);
            all.Sort((a, b) => a.SequenceNumber.CompareTo(b.SequenceNumber));
            return all;
        }
    }

    /// <summary>
    /// Gets all recorded calls that have not been matched by a verification statement.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IReadOnlyList<CallRecord> GetUnverifiedCalls()
    {
        lock (Lock)
        {
            if (_callsByMemberId is null) return [];
            var result = new List<CallRecord>();
            CollectCallRecords(result, static r => !r.IsVerified);
            return result;
        }
    }

    /// <summary>
    /// Gets all registered setups.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IReadOnlyList<MethodSetup> GetSetups()
    {
        if (_hasStaleSetups)
        {
            RebuildStaleSnapshots();
        }

        var snapshot = _setupsByMemberId;
        if (snapshot is null)
        {
            return [];
        }

        var all = new List<MethodSetup>();
        foreach (var arr in snapshot)
        {
            if (arr is not null)
            {
                all.AddRange(arr);
            }
        }
        return all;
    }

    /// <summary>
    /// Returns a diagnostic report of this mock's setup coverage and call matching.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Diagnostics.MockDiagnostics GetDiagnostics()
    {
        var setups = GetSetups();
        var unusedSetups = new List<Diagnostics.SetupInfo>();
        var totalSetups = setups.Count;
        var exercisedSetups = 0;

        foreach (var setup in setups)
        {
            if (setup.InvokeCount > 0)
            {
                exercisedSetups++;
            }
            else
            {
                unusedSetups.Add(new Diagnostics.SetupInfo(
                    setup.MemberId,
                    setup.MemberName,
                    setup.GetMatcherDescriptions(),
                    setup.InvokeCount));
            }
        }

        var unmatchedCalls = new List<CallRecord>();
        lock (Lock)
        {
            CollectCallRecords(unmatchedCalls, static r => r.IsUnmatched);
        }

        return new Diagnostics.MockDiagnostics(unusedSetups, unmatchedCalls, totalSetups, exercisedSetups);
    }

    /// <summary>
    /// Tries to get a cached auto-mock by its cache key. Used by Mock&lt;T&gt;.GetAutoMock.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryGetAutoMock(string cacheKey, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IMock? mock)
    {
        if (Volatile.Read(ref _autoMockCache) is not { } cache)
        {
            mock = null;
            return false;
        }
        return cache.TryGetValue(cacheKey, out mock);
    }

    /// <summary>
    /// Clears all setups and call history.
    /// </summary>
    public void Reset()
    {
        lock (Lock)
        {
            _setupsByMemberId = null;
            _setupListsByMemberId = null;
            _hasStatefulSetups = false;
            _hasStaleSetups = false;
            _currentState = null;
            PendingRequiredState = null;
            _callsByMemberId = null;
            _callCountByMemberId = null;
        }
        Volatile.Write(ref _autoTrackValues, null);
        Volatile.Write(ref _eventSubscriptions, null);
        Volatile.Write(ref _onSubscribeCallbacks, null);
        Volatile.Write(ref _onUnsubscribeCallbacks, null);
        Volatile.Write(ref _autoMockCache, null);
    }

    /// <summary>
    /// Registers a callback to invoke when an event is subscribed to.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void OnSubscribe(string eventName, Action callback)
    {
        OnSubscribeCallbacks[eventName] = callback;
    }

    /// <summary>
    /// Registers a callback to invoke when an event is unsubscribed from.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void OnUnsubscribe(string eventName, Action callback)
    {
        OnUnsubscribeCallbacks[eventName] = callback;
    }

    /// <summary>
    /// Records an event subscription (add) or unsubscription (remove). Called by generated event accessors.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RecordEventSubscription(string eventName, bool isSubscribe)
    {
        EventSubscriptions.Enqueue((eventName, isSubscribe));

        if (isSubscribe)
        {
            if (Volatile.Read(ref _onSubscribeCallbacks) is { } subCallbacks && subCallbacks.TryGetValue(eventName, out var callback))
            {
                callback();
            }
        }
        else
        {
            if (Volatile.Read(ref _onUnsubscribeCallbacks) is { } unsubCallbacks && unsubCallbacks.TryGetValue(eventName, out var callback))
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
        if (Volatile.Read(ref _eventSubscriptions) is not { } subs) return 0;
        int count = 0;
        foreach (var (name, isSub) in subs)
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
        if (Volatile.Read(ref _eventSubscriptions) is not { } subs) return false;
        foreach (var (name, isSub) in subs)
        {
            if (name == eventName && isSub) return true;
        }
        return false;
    }


    /// <summary>
    /// Collects call records from all per-member lists into <paramref name="target"/>,
    /// optionally filtered by <paramref name="predicate"/>. Must be called under <see cref="Lock"/>.
    /// </summary>
    private void CollectCallRecords(List<CallRecord> target, Predicate<CallRecord>? predicate = null)
    {
        if (_callsByMemberId is null) return;
        foreach (var list in _callsByMemberId)
        {
            if (list is null) continue;
            if (predicate is null)
            {
                target.AddRange(list);
            }
            else
            {
                foreach (var record in list)
                {
                    if (predicate(record))
                    {
                        target.Add(record);
                    }
                }
            }
        }
    }

    private CallRecord RecordCall(int memberId, string memberName, object?[] args)
    {
        var seq = MockCallSequence.Next();
        var record = new CallRecord(memberId, memberName, args, seq);
        lock (Lock)
        {
            EnsureCallArrayCapacity(memberId);
            var memberCalls = _callsByMemberId![memberId] ??= new();
            memberCalls.Add(record);
            _callCountByMemberId![memberId]++;
        }
        return record;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureCallArrayCapacity(int memberId)
    {
        var required = memberId + 1;
        if (_callsByMemberId is null || _callsByMemberId.Length < required)
        {
            var newSize = _callsByMemberId is null
                ? Math.Max(required, 8)
                : Math.Max(required, _callsByMemberId.Length * 2);
            var newByMember = new List<CallRecord>?[newSize];
            var newCounts = new int[newSize];
            if (_callsByMemberId is not null)
            {
                Array.Copy(_callsByMemberId, newByMember, _callsByMemberId.Length);
                Array.Copy(_callCountByMemberId!, newCounts, _callCountByMemberId!.Length);
            }
            _callsByMemberId = newByMember;
            _callCountByMemberId = newCounts;
        }
    }

    /// <summary>
    /// Applies out/ref assignments and raises events for a matched setup.
    /// Consolidates the pattern that appeared in all four Handle methods.
    /// </summary>
    private void ApplyMatchedSetup(MethodSetup? matchedSetup)
    {
        // Always set (or clear) OutRefContext — even when matchedSetup is null.
        // A reentrant mock call inside Execute() may have written to it, and the
        // outer call must overwrite that to avoid stale thread-local state.
        OutRefContext.Set(matchedSetup?.OutRefAssignments);
        if (matchedSetup is not null)
        {
            RaiseEventsForSetup(matchedSetup);
        }
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

    /// <summary>
    /// Rebuilds snapshot arrays from the mutable lists for all members.
    /// Called once on transition from setup phase to invocation phase.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RebuildStaleSnapshots()
    {
        lock (Lock)
        {
            if (!_hasStaleSetups) return;
            var lists = _setupListsByMemberId;
            var snapshots = _setupsByMemberId;
            if (lists is not null && snapshots is not null)
            {
                for (int i = 0; i < lists.Length; i++)
                {
                    if (lists[i] is { } list)
                    {
                        snapshots[i] = list.ToArray();
                    }
                }
            }
            _hasStaleSetups = false;
        }
    }

    private (bool SetupFound, IBehavior? Behavior, MethodSetup? Setup) FindMatchingSetup(int memberId, object?[] args)
    {
        // Rebuild snapshots if setup phase just ended (batches all ToArray work into one pass)
        if (_hasStaleSetups)
        {
            RebuildStaleSnapshots();
        }

        // When state machine features are in use, serialize the full match-and-transition
        // to prevent concurrent invocations from consuming the same state transition
        if (_hasStatefulSetups)
        {
            return FindMatchingSetupLocked(memberId, args);
        }

        var snapshot = _setupsByMemberId;
        if (snapshot is null || (uint)memberId >= (uint)snapshot.Length)
        {
            return (false, null, null);
        }

        var setups = snapshot[memberId];
        if (setups is null)
        {
            return (false, null, null);
        }

        // Iterate last-added-first to implement "last wins" semantics
        for (int i = setups.Length - 1; i >= 0; i--)
        {
            var setup = setups[i];

            if (setup.Matches(args))
            {
                setup.IncrementInvokeCount();
                setup.ApplyCaptures(args);
                return (true, setup.GetNextBehavior(), setup);
            }
        }

        return (false, null, null);
    }

    private (bool SetupFound, IBehavior? Behavior, MethodSetup? Setup) FindMatchingSetupLocked(int memberId, object?[] args)
    {
        lock (Lock)
        {
            if (_setupListsByMemberId is not { } lists || (uint)memberId >= (uint)lists.Length)
            {
                return (false, null, null);
            }

            var setups = lists[memberId];
            if (setups is null)
            {
                return (false, null, null);
            }

            for (int i = setups.Count - 1; i >= 0; i--)
            {
                var setup = setups[i];

                if (setup.RequiredState is not null && setup.RequiredState != _currentState)
                {
                    continue;
                }

                if (setup.Matches(args))
                {
                    setup.IncrementInvokeCount();
                    setup.ApplyCaptures(args);
                    if (setup.TransitionTarget is not null)
                    {
                        _currentState = setup.TransitionTarget;
                    }
                    return (true, setup.GetNextBehavior(), setup);
                }
            }
        }

        return (false, null, null);
    }

    private static string FormatCall(string memberName, object?[] args)
    {
        var formattedArgs = string.Join(", ", args.Select(a => a?.ToString() ?? "null"));
        return $"{memberName}({formattedArgs})";
    }
}
