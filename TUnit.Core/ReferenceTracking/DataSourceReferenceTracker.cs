using System.Collections.Concurrent;

namespace TUnit.Core.ReferenceTracking;

/// <summary>
/// Tracks references to objects created from data sources to manage their lifecycle.
/// </summary>
public class DataSourceReferenceTracker
{
    private readonly ConcurrentDictionary<object, ReferenceInfo> _references = new();
    
    /// <summary>
    /// Information about a tracked reference
    /// </summary>
    public class ReferenceInfo
    {
        private int _activeCount;
        private readonly object _lock = new();
        
        public Type ObjectType { get; }
        public DateTime FirstTracked { get; }
        public DateTime? LastAccessed { get; private set; }
        
        public ReferenceInfo(Type objectType)
        {
            ObjectType = objectType;
            FirstTracked = DateTime.UtcNow;
            _activeCount = 0;
        }
        
        public int ActiveCount 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _activeCount; 
                } 
            } 
        }
        
        public int Increment()
        {
            lock (_lock)
            {
                LastAccessed = DateTime.UtcNow;
                return ++_activeCount;
            }
        }
        
        public int Decrement()
        {
            lock (_lock)
            {
                LastAccessed = DateTime.UtcNow;
                return --_activeCount;
            }
        }
    }
    
    /// <summary>
    /// Tracks an object created from a data source and increments its reference count.
    /// </summary>
    /// <param name="obj">The object to track</param>
    /// <returns>The tracked object (same instance)</returns>
    public T TrackObject<T>(T obj) where T : notnull
    {
        var info = _references.GetOrAdd(obj, 
            _ => new ReferenceInfo(obj.GetType()));
        
        info.Increment();
        
        return obj;
    }
    
    /// <summary>
    /// Decrements the reference count for an object and returns whether it can be disposed.
    /// </summary>
    /// <param name="obj">The object to release</param>
    /// <returns>True if the object has no more references and can be disposed</returns>
    public async Task<bool> ReleaseObject(object? obj)
    {
        if (obj == null)
        {
            return false;
        }
        
        if (!_references.TryGetValue(obj, out var info))
        {
            return false;
        }
        
        var count = info.Decrement();
        
        if (count <= 0)
        {
            _references.TryRemove(obj, out _);
            
            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets information about a tracked object.
    /// </summary>
    public ReferenceInfo? GetReferenceInfo(object obj)
    {
        return obj != null && _references.TryGetValue(obj, out var info) ? info : null;
    }
    
    /// <summary>
    /// Gets all currently tracked objects and their reference counts.
    /// </summary>
    public IReadOnlyDictionary<object, ReferenceInfo> GetAllTrackedObjects()
    {
        return _references;
    }
    
    /// <summary>
    /// Gets the count of currently tracked objects.
    /// </summary>
    public int TrackedObjectCount => _references.Count;
    
    /// <summary>
    /// Gets the total number of active references across all objects.
    /// </summary>
    public int TotalActiveReferences => _references.Values.Sum(info => info.ActiveCount);
    
    /// <summary>
    /// Clears all tracked references. Use with caution!
    /// </summary>
    public void Clear()
    {
        _references.Clear();
    }
}