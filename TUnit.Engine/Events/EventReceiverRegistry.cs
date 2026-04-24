using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Events;

/// Fast registry for event receiver presence checks using bit flags.
/// Thread-safe without locks: uses ConcurrentDictionary + copy-on-write arrays
/// updated via AddOrUpdate. Reads are lock-free.
internal sealed class EventReceiverRegistry
{
    // Bit flags for fast checking
    [Flags]
    private enum EventTypes
    {
        None = 0,
        TestRegistered = 1 << 0,
        TestStart = 1 << 1,
        TestEnd = 1 << 2,
        TestSkipped = 1 << 3,
        FirstTestInSession = 1 << 4,
        LastTestInSession = 1 << 5,
        FirstTestInAssembly = 1 << 6,
        LastTestInAssembly = 1 << 7,
        FirstTestInClass = 1 << 8,
        LastTestInClass = 1 << 9,
        All = ~0
    }

    // _registeredEvents is volatile for publication semantics; updated via Interlocked OR.
    private int _registeredEvents;

    // Copy-on-write: each value is an immutable snapshot array. Writers replace the
    // entry via AddOrUpdate with a new array that contains the appended receiver.
    private readonly ConcurrentDictionary<Type, object[]> _receiversByType = new();

    // Cache of typed arrays (T[]) built on first read per type. Invalidated by
    // writers removing the entry for the affected interface type.
    private readonly ConcurrentDictionary<Type, Array> _cachedTypedReceivers = new();

    /// <summary>
    /// Register event receivers from a collection of objects.
    /// </summary>
    public void RegisterReceivers(ReadOnlySpan<object> objects)
    {
        foreach (var obj in objects)
        {
            RegisterReceiverInternal(obj);
        }
    }

    /// <summary>
    /// Register a single event receiver.
    /// </summary>
    public void RegisterReceiver(object receiver)
    {
        RegisterReceiverInternal(receiver);
    }

    private void RegisterReceiverInternal(object receiver)
    {
        UpdateEventFlags(receiver);

        // Register for each interface type the object implements.
        RegisterIfImplements<ITestStartEventReceiver>(receiver);
        RegisterIfImplements<ITestEndEventReceiver>(receiver);
        RegisterIfImplements<ITestSkippedEventReceiver>(receiver);
        RegisterIfImplements<ITestRegisteredEventReceiver>(receiver);
        RegisterIfImplements<IFirstTestInTestSessionEventReceiver>(receiver);
        RegisterIfImplements<ILastTestInTestSessionEventReceiver>(receiver);
        RegisterIfImplements<IFirstTestInAssemblyEventReceiver>(receiver);
        RegisterIfImplements<ILastTestInAssemblyEventReceiver>(receiver);
        RegisterIfImplements<IFirstTestInClassEventReceiver>(receiver);
        RegisterIfImplements<ILastTestInClassEventReceiver>(receiver);
    }

    private void RegisterIfImplements<T>(object receiver) where T : class
    {
        if (receiver is not T)
        {
            return;
        }

        var interfaceType = typeof(T);

        // Copy-on-write append. AddOrUpdate guarantees the updateValueFactory is
        // retried until the CAS succeeds, so concurrent writers do not lose updates.
        _receiversByType.AddOrUpdate(
            interfaceType,
            _ => [receiver],
            (_, existing) =>
            {
                var newArray = new object[existing.Length + 1];
                existing.CopyTo(newArray, 0);
                newArray[existing.Length] = receiver;
                return newArray;
            });

        // Invalidate only the changed type instead of clearing the entire cache.
        // The next read of GetReceiversOfType<T> will rebuild from the freshest
        // snapshot in _receiversByType.
        _cachedTypedReceivers.TryRemove(interfaceType, out _);
    }

    /// <summary>
    /// Fast check if any receivers registered for event type
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTestStartReceivers() => (Volatile.Read(ref _registeredEvents) & (int)EventTypes.TestStart) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTestEndReceivers() => (Volatile.Read(ref _registeredEvents) & (int)EventTypes.TestEnd) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTestSkippedReceivers() => (Volatile.Read(ref _registeredEvents) & (int)EventTypes.TestSkipped) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTestRegisteredReceivers() => (Volatile.Read(ref _registeredEvents) & (int)EventTypes.TestRegistered) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasFirstTestInSessionReceivers() => (Volatile.Read(ref _registeredEvents) & (int)EventTypes.FirstTestInSession) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasLastTestInSessionReceivers() => (Volatile.Read(ref _registeredEvents) & (int)EventTypes.LastTestInSession) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasFirstTestInAssemblyReceivers() => (Volatile.Read(ref _registeredEvents) & (int)EventTypes.FirstTestInAssembly) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasLastTestInAssemblyReceivers() => (Volatile.Read(ref _registeredEvents) & (int)EventTypes.LastTestInAssembly) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasFirstTestInClassReceivers() => (Volatile.Read(ref _registeredEvents) & (int)EventTypes.FirstTestInClass) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasLastTestInClassReceivers() => (Volatile.Read(ref _registeredEvents) & (int)EventTypes.LastTestInClass) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasAnyReceivers() => Volatile.Read(ref _registeredEvents) != (int)EventTypes.None;

    public T[] GetReceiversOfType<T>() where T : class
    {
        var typeKey = typeof(T);

        while (true)
        {
            // Lock-free fast path: cache hit (common case after warmup).
            if (_cachedTypedReceivers.TryGetValue(typeKey, out var cached))
            {
                return (T[])cached;
            }

            // Cache miss. Snapshot the source so we can validate via identity after publish.
            _receiversByType.TryGetValue(typeKey, out var sourceSnapshot);
            var typedArray = BuildTypedArray<T>(sourceSnapshot);

            var published = (T[])_cachedTypedReceivers.GetOrAdd(typeKey, typedArray);

            // If a writer raced with us (mutated source between our snapshot and publish),
            // the writer's TryRemove-on-cache could have been ordered before our GetOrAdd,
            // leaving a stale value cached. Detect by re-reading source: if it changed,
            // evict our entry so the next reader rebuilds.
            _receiversByType.TryGetValue(typeKey, out var latest);
            if (!ReferenceEquals(latest, sourceSnapshot))
            {
                if (ReferenceEquals(published, typedArray))
                {
                    ((ICollection<KeyValuePair<Type, Array>>)_cachedTypedReceivers)
                        .Remove(new KeyValuePair<Type, Array>(typeKey, typedArray));
                }
                continue;
            }

            return published;
        }
    }

    private static T[] BuildTypedArray<T>(object[]? source) where T : class
    {
        if (source is null || source.Length == 0)
        {
            return [];
        }

        var typedArray = new T[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            typedArray[i] = (T)source[i];
        }
        return typedArray;
    }

    private void UpdateEventFlags(object receiver)
    {
        var flags = EventTypes.None;
        if (receiver is ITestStartEventReceiver)
        {
            flags |= EventTypes.TestStart;
        }
        if (receiver is ITestEndEventReceiver)
        {
            flags |= EventTypes.TestEnd;
        }
        if (receiver is ITestSkippedEventReceiver)
        {
            flags |= EventTypes.TestSkipped;
        }
        if (receiver is ITestRegisteredEventReceiver)
        {
            flags |= EventTypes.TestRegistered;
        }
        if (receiver is IFirstTestInTestSessionEventReceiver)
        {
            flags |= EventTypes.FirstTestInSession;
        }
        if (receiver is ILastTestInTestSessionEventReceiver)
        {
            flags |= EventTypes.LastTestInSession;
        }
        if (receiver is IFirstTestInAssemblyEventReceiver)
        {
            flags |= EventTypes.FirstTestInAssembly;
        }
        if (receiver is ILastTestInAssemblyEventReceiver)
        {
            flags |= EventTypes.LastTestInAssembly;
        }
        if (receiver is IFirstTestInClassEventReceiver)
        {
            flags |= EventTypes.FirstTestInClass;
        }
        if (receiver is ILastTestInClassEventReceiver)
        {
            flags |= EventTypes.LastTestInClass;
        }

        if (flags == EventTypes.None)
        {
            return;
        }

        // Atomic OR without a lock. Retries on contention.
        var current = Volatile.Read(ref _registeredEvents);
        while (true)
        {
            var desired = current | (int)flags;
            if (desired == current)
            {
                return; // Already set.
            }
            var actual = Interlocked.CompareExchange(ref _registeredEvents, desired, current);
            if (actual == current)
            {
                return;
            }
            current = actual;
        }
    }
}
