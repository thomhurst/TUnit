using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Events;

/// Fast registry for event receiver presence checks using bit flags
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
    
    private volatile EventTypes _registeredEvents = EventTypes.None;
    private readonly Dictionary<Type, object[]> _receiversByType = new();
    private readonly Dictionary<Type, Array> _cachedTypedReceivers = new(); // Cache typed arrays
    private readonly ReaderWriterLockSlim _lock = new();
    
    /// <summary>
    /// Register event receivers from a collection of objects
    /// </summary>
    public void RegisterReceivers(IEnumerable<object> objects)
    {
        _lock.EnterWriteLock();
        try
        {
            foreach (var obj in objects)
            {
                RegisterReceiverInternal(obj);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    /// <summary>
    /// Register a single event receiver
    /// </summary>
    public void RegisterReceiver(object receiver)
    {
        _lock.EnterWriteLock();
        try
        {
            RegisterReceiverInternal(receiver);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    private void RegisterReceiverInternal(object receiver)
    {
        UpdateEventFlags(receiver);

        // Register for each interface type the object implements
        // We use a simpler approach that doesn't require reflection
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

        // Invalidate cache when new receivers are added
        _cachedTypedReceivers.Clear();
    }
    
    private void RegisterIfImplements<T>(object receiver) where T : class
    {
        if (receiver is T)
        {
            var interfaceType = typeof(T);
            if (_receiversByType.TryGetValue(interfaceType, out var existing))
            {
                var newArray = new object[existing.Length + 1];
                existing.CopyTo(newArray, 0);
                newArray[^1] = receiver;
                _receiversByType[interfaceType] = newArray;
            }
            else
            {
                _receiversByType[interfaceType] = [receiver];
            }
        }
    }
    
    /// <summary>
    /// Fast check if any receivers registered for event type
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTestStartReceivers() => (_registeredEvents & EventTypes.TestStart) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTestEndReceivers() => (_registeredEvents & EventTypes.TestEnd) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTestSkippedReceivers() => (_registeredEvents & EventTypes.TestSkipped) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTestRegisteredReceivers() => (_registeredEvents & EventTypes.TestRegistered) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasFirstTestInSessionReceivers() => (_registeredEvents & EventTypes.FirstTestInSession) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasLastTestInSessionReceivers() => (_registeredEvents & EventTypes.LastTestInSession) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasFirstTestInAssemblyReceivers() => (_registeredEvents & EventTypes.FirstTestInAssembly) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasLastTestInAssemblyReceivers() => (_registeredEvents & EventTypes.LastTestInAssembly) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasFirstTestInClassReceivers() => (_registeredEvents & EventTypes.FirstTestInClass) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasLastTestInClassReceivers() => (_registeredEvents & EventTypes.LastTestInClass) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasAnyReceivers() => _registeredEvents != EventTypes.None;
    
    /// <summary>
    /// Get receivers of specific type (for invocation)
    /// Caches typed arrays to avoid repeated allocations and casts
    /// </summary>
    public T[] GetReceiversOfType<T>() where T : class
    {
        var typeKey = typeof(T);

        _lock.EnterReadLock();
        try
        {
            // Check cache first (hot path optimization)
            if (_cachedTypedReceivers.TryGetValue(typeKey, out var cached))
            {
                return (T[])cached;
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // Cache miss - need to create typed array
        _lock.EnterUpgradeableReadLock();
        try
        {
            // Double-check in case another thread populated cache
            if (_cachedTypedReceivers.TryGetValue(typeKey, out var cached))
            {
                return (T[])cached;
            }

            // Create and cache typed array
            if (_receiversByType.TryGetValue(typeKey, out var receivers))
            {
                var typedArray = new T[receivers.Length];
                for (var i = 0; i < receivers.Length; i++)
                {
                    typedArray[i] = (T)receivers[i];
                }

                _lock.EnterWriteLock();
                try
                {
                    _cachedTypedReceivers[typeKey] = typedArray;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                return typedArray;
            }

            // No receivers registered for this type
            T[] emptyArray = [];
            _lock.EnterWriteLock();
            try
            {
                _cachedTypedReceivers[typeKey] = emptyArray;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            return emptyArray;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }
    
    private void UpdateEventFlags(object receiver)
    {
        if (receiver is ITestStartEventReceiver)
        {
            _registeredEvents |= EventTypes.TestStart;
        }
        if (receiver is ITestEndEventReceiver)
        {
            _registeredEvents |= EventTypes.TestEnd;
        }
        if (receiver is ITestSkippedEventReceiver)
        {
            _registeredEvents |= EventTypes.TestSkipped;
        }
        if (receiver is ITestRegisteredEventReceiver)
        {
            _registeredEvents |= EventTypes.TestRegistered;
        }
        if (receiver is IFirstTestInTestSessionEventReceiver)
        {
            _registeredEvents |= EventTypes.FirstTestInSession;
        }
        if (receiver is ILastTestInTestSessionEventReceiver)
        {
            _registeredEvents |= EventTypes.LastTestInSession;
        }
        if (receiver is IFirstTestInAssemblyEventReceiver)
        {
            _registeredEvents |= EventTypes.FirstTestInAssembly;
        }
        if (receiver is ILastTestInAssemblyEventReceiver)
        {
            _registeredEvents |= EventTypes.LastTestInAssembly;
        }
        if (receiver is IFirstTestInClassEventReceiver)
        {
            _registeredEvents |= EventTypes.FirstTestInClass;
        }
        if (receiver is ILastTestInClassEventReceiver)
        {
            _registeredEvents |= EventTypes.LastTestInClass;
        }
    }
    
    
    public void Dispose()
    {
        _lock?.Dispose();
    }
}