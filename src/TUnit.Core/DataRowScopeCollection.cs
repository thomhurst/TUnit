using TUnit.Core.Discovery;

namespace TUnit.Core;

/// <summary>
/// Owns a row's scopes. Object lookup releases each scope at its first resolved object's graph
/// depth; completion releases scopes left by failed factories or untracked values.
/// </summary>
internal sealed class DataRowScopeCollection
{
    private readonly object _lock = new();
    private readonly List<ScopeOwner> _owners = [];
    private readonly Dictionary<object, List<ScopeOwner>> _objects = new(Helpers.ReferenceEqualityComparer.Instance);

    internal ScopeOwner Register(object? scope)
    {
        var owner = new ScopeOwner(scope);
        lock (_lock)
            _owners.Add(owner);
        return owner;
    }

    // A scope resolving several objects is released at the first object's graph depth;
    // subsequent releases share the same once-only owner.
    internal void RegisterObject(ScopeOwner owner, object? instance)
    {
        if (instance is null)
        {
            return;
        }

        owner.RegisterObject(instance);
        lock (_lock)
        {
            if (!_objects.TryGetValue(instance, out var owners))
                _objects.Add(instance, owners = []);
            if (!owners.Contains(owner))
                owners.Add(owner);
        }
    }

    internal ScopeOwner[] GetOwners()
    {
        lock (_lock) return _owners.ToArray();
    }

    internal void CaptureOwnership(IEnumerable<object?>? injectedRoots = null)
    {
        foreach (var owner in GetOwners()) owner.CaptureOwnership(injectedRoots ?? []);
    }

    internal DataRowScopeCollection Clone()
    {
        var clone = new DataRowScopeCollection();
        clone.AddRange(this);
        return clone;
    }

    internal void AddRange(DataRowScopeCollection scopes)
    {
        // Snapshot the source before locking the destination to avoid lock-order inversions.
        ScopeOwner[] owners;
        KeyValuePair<object, ScopeOwner[]>[] objects;
        lock (scopes._lock)
        {
            owners = scopes._owners.ToArray();
            objects = scopes._objects.Select(pair =>
                new KeyValuePair<object, ScopeOwner[]>(pair.Key, pair.Value.ToArray())).ToArray();
        }
        lock (_lock)
        {
            _owners.AddRange(owners);
            foreach (var pair in objects)
                foreach (var owner in pair.Value)
                    RegisterObject(owner, pair.Key);
        }
    }

    internal ValueTask<List<Exception>?> ReleaseAsync(object instance, List<Exception>? exceptions = null)
    {
        ScopeOwner[] owners;
        lock (_lock)
            owners = _objects.TryGetValue(instance, out var registered) ? registered.ToArray() : [];
        return DisposeAsync(owners, exceptions);
    }

    internal ValueTask<List<Exception>?> CompleteAsync(List<Exception>? exceptions = null)
    {
        ScopeOwner[] owners;
        lock (_lock)
            owners = _owners.ToArray();
        return DisposeAsync(owners, exceptions);
    }

    private static async ValueTask<List<Exception>?> DisposeAsync(ScopeOwner[] owners, List<Exception>? exceptions)
    {
        foreach (var owner in owners)
        {
            try
            {
                await owner.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                (exceptions ??= []).Add(exception);
            }
        }

        return exceptions;
    }

    /// <summary>Shared by cloned rows; scope disposal is attempted once, including when it throws.</summary>
    internal sealed class ScopeOwner
    {
        private readonly object? _scope;
        internal bool OwnsCreatedObjects { get; set; }
        // Keep exclusions from wider graphs when a foreign row refreshes only an owned child.
        private HashSet<object>? _injectedObjects;
        private readonly object _ownershipLock = new();
        private readonly HashSet<object> _roots = new(Helpers.ReferenceEqualityComparer.Instance);
        private readonly HashSet<object> _pinnedObjects = new(Helpers.ReferenceEqualityComparer.Instance);
        private HashSet<object>? _ownedObjects;
        private int _disposed;

        internal ScopeOwner(object? scope)
        {
            _scope = scope;
            OwnsCreatedObjects = scope is IDisposable or IAsyncDisposable;
            // The row always disposes its scope, even when resolved services stay tracker-owned.
            if (scope is IDisposable or IAsyncDisposable)
                ExternalObjectOwnership.Register(scope, this);
        }

        internal void RegisterObject(object instance)
        {
            if (!OwnsCreatedObjects) return;
            bool added;
            lock (_ownershipLock) added = _roots.Add(instance);
            if (added)
            {
                ExternalObjectOwnership.Register(instance, this);
                RetainWhileCounted(instance);
            }
        }

        internal bool Owns(object instance)
        {
            lock (_ownershipLock)
                return ReferenceEquals(instance, _scope) || _roots.Contains(instance) || _ownedObjects?.Contains(instance) == true;
        }

        // Snapshots accumulate dependencies for the lifetime of this handle: a container still
        // owns previously exposed dependencies after a shared root changes its graph.
        internal void CaptureOwnership(IEnumerable<object?> injectedRoots)
        {
            object[] roots;
            lock (_ownershipLock) roots = _roots.ToArray();
            CaptureOwnership(roots, injectedRoots);
        }

        // A foreign row consumes only this graph; other roots may already be disposed.
        internal void CaptureOwnershipFor(object root, IEnumerable<object?> injectedRoots)
            => CaptureOwnership([root], injectedRoots);

        private void CaptureOwnership(object[] roots, IEnumerable<object?> injectedRoots)
        {
            if (!OwnsCreatedObjects || roots.Length == 0) return;
            // Publish a complete snapshot only after discovery succeeds. Traversal itself has no
            // ownership side effects, and readers never observe a partially built graph.
            HashSet<object> injected;
            lock (_ownershipLock)
                injected = _injectedObjects is null
                    ? new(Helpers.ReferenceEqualityComparer.Instance)
                    : new(_injectedObjects, Helpers.ReferenceEqualityComparer.Instance);
            var owned = ObjectGraphDiscoverer.DiscoverOwnedObjects(roots, injectedRoots, injected);
            lock (_ownershipLock)
            {
                // A shared service can expose a different dependency on a later row. Its scope
                // still owns objects already registered through an earlier snapshot.
                if (_injectedObjects is not null) injected.UnionWith(_injectedObjects);
                _injectedObjects = injected.Count == 0 ? null : injected;
                if (_ownedObjects is not null) owned.UnionWith(_ownedObjects);
                owned.ExceptWith(injected);
                _ownedObjects = owned;
            }
            foreach (var instance in owned)
            {
                ExternalObjectOwnership.Register(instance, this);
                RetainWhileCounted(instance);
            }
        }

        // An unrelated source may already hold this service in another session. Its existing
        // reference keeps the owner handle alive even after this row/session has completed.
        // The generic count callback releases that handle with the last reference, not forever.
        internal void RetainWhileCounted(object instance)
        {
            if (instance is not IDisposable and not IAsyncDisposable) return;
            lock (_ownershipLock)
            {
                if (!_pinnedObjects.Add(instance)) return;
            }
            // Registration may invoke the callback immediately. Never hold the ownership lock
            // while entering the counter, whose callbacks also run during tracker cleanup.
            Tracking.ObjectTracker.OnDisposed(instance, () =>
            {
                lock (_ownershipLock) _pinnedObjects.Remove(instance);
            });
        }

        internal async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            if (_scope is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (_scope is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
