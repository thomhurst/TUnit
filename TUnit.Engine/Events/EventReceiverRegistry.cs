using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Events;

/// <summary>
/// Fast registry for event receiver presence checks
/// </summary>
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
    /// </summary>
    public T[] GetReceiversOfType<T>() where T : class
    {
        _lock.EnterReadLock();
        try
        {
            if (_receiversByType.TryGetValue(typeof(T), out var receivers))
            {
                // Cast array to specific type
                var typedArray = new T[receivers.Length];
                for (int i = 0; i < receivers.Length; i++)
                {
                    typedArray[i] = (T)receivers[i];
                }
                return typedArray;
            }
            return [
            ];
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    private void UpdateEventFlags(object receiver)
    {
        // Set flags based on implemented interfaces
        if (receiver is ITestStartEventReceiver)
            _registeredEvents |= EventTypes.TestStart;
        if (receiver is ITestEndEventReceiver)
            _registeredEvents |= EventTypes.TestEnd;
        if (receiver is ITestSkippedEventReceiver)
            _registeredEvents |= EventTypes.TestSkipped;
        if (receiver is ITestRegisteredEventReceiver)
            _registeredEvents |= EventTypes.TestRegistered;
        if (receiver is IFirstTestInTestSessionEventReceiver)
            _registeredEvents |= EventTypes.FirstTestInSession;
        if (receiver is ILastTestInTestSessionEventReceiver)
            _registeredEvents |= EventTypes.LastTestInSession;
        if (receiver is IFirstTestInAssemblyEventReceiver)
            _registeredEvents |= EventTypes.FirstTestInAssembly;
        if (receiver is ILastTestInAssemblyEventReceiver)
            _registeredEvents |= EventTypes.LastTestInAssembly;
        if (receiver is IFirstTestInClassEventReceiver)
            _registeredEvents |= EventTypes.FirstTestInClass;
        if (receiver is ILastTestInClassEventReceiver)
            _registeredEvents |= EventTypes.LastTestInClass;
    }
    
    
    public void Dispose()
    {
        _lock?.Dispose();
    }
}