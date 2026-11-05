using System.Runtime.CompilerServices;
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
        // Owners already registered these roots in the source; copying lookup entries must not
        // re-enter the tracker while holding the destination collection lock.
        lock (_lock)
        {
            _owners.AddRange(owners);
            foreach (var pair in objects)
            {
                if (!_objects.TryGetValue(pair.Key, out var registered))
                    _objects.Add(pair.Key, registered = []);
                foreach (var owner in pair.Value)
                    if (!registered.Contains(owner)) registered.Add(owner);
            }
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
        private readonly ConditionalWeakTable<object, object> _injectedObjects = new();
        private readonly List<WeakReference<object>> _injectedIdentities = [];
        private readonly object _ownershipLock = new();
        private readonly HashSet<object> _roots = new(Helpers.ReferenceEqualityComparer.Instance);
        private readonly HashSet<object> _pinnedObjects = new(Helpers.ReferenceEqualityComparer.Instance);
        private readonly ConditionalWeakTable<object, object> _ownedObjects = new();
        private static readonly object Membership = new();
        private int _disposed;

        internal ScopeOwner(object? scope)
        {
            _scope = scope;
            OwnsCreatedObjects = scope is IDisposable or IAsyncDisposable;
            // The row always disposes its scope, even when resolved services stay tracker-owned.
            if (scope is IDisposable or IAsyncDisposable)
            {
                ExternalObjectOwnership.Register(scope, this);
                RetainWhileCounted(scope);
            }
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
                return ReferenceEquals(instance, _scope) || _roots.Contains(instance) || _ownedObjects.TryGetValue(instance, out _);
        }

        // Remember previously exposed identities weakly: the container owns their lifetime.
        // A changed graph must not make this handle retain otherwise unreachable dependencies.
        internal void CaptureOwnership(IEnumerable<object?> injectedRoots)
        {
            object[] roots;
            lock (_ownershipLock) roots = _roots.ToArray();
            CaptureOwnership(roots, injectedRoots);
        }

        // A foreign row consumes only this graph; other roots may already be disposed.
        internal void CaptureOwnershipFor(object root, IEnumerable<object?> injectedRoots, bool retainPartialGraphOnError = false)
            => CaptureOwnership([root], injectedRoots, retainPartialGraphOnError);

        private void CaptureOwnership(object[] roots, IEnumerable<object?> injectedRoots, bool retainPartialGraphOnError = false)
        {
            if (!OwnsCreatedObjects || roots.Length == 0) return;
            // Discover outside the ownership lock: user getters can enter tracker code.
            // Publish after discovery (or best-effort failed-factory traversal), preserving concurrent exclusions.
            var injected = new HashSet<object>(Helpers.ReferenceEqualityComparer.Instance);
            lock (_ownershipLock)
                CollectInjectedIdentities(injected);
            var owned = ObjectGraphDiscoverer.DiscoverOwnedObjects(roots, injectedRoots, injected, retainPartialGraphOnError);
            lock (_ownershipLock)
            {
                CollectInjectedIdentities(injected);
                foreach (var instance in injected)
                {
                    if (!_injectedObjects.TryGetValue(instance, out _))
                    {
                        _injectedObjects.Add(instance, Membership);
                        _injectedIdentities.Add(new WeakReference<object>(instance));
                    }
                    // Explicit scope and resolved roots always remain container-owned.
                    if (ReferenceEquals(instance, _scope) || _roots.Contains(instance)) continue;
                    if (_ownedObjects.Remove(instance))
                        ExternalObjectOwnership.Unregister(instance, this);
                }
                owned.ExceptWith(injected);
                foreach (var instance in owned)
                {
                    if (_ownedObjects.TryGetValue(instance, out _)) continue;
                    _ownedObjects.Add(instance, Membership);
                    ExternalObjectOwnership.Register(instance, this);
                }
            }
            // Lock order is tracker counter -> ownership -> registry OwnerSet. Never enter
            // a tracker counter while holding the ownership lock: callbacks can re-enter it.
            foreach (var instance in owned) RetainWhileCounted(instance);
        }

        // Called under the ownership lock. Compact dead identities without retaining their targets.
        private void CollectInjectedIdentities(HashSet<object> injected)
        {
            for (var i = _injectedIdentities.Count - 1; i >= 0; i--)
            {
                if (_injectedIdentities[i].TryGetTarget(out var instance)) injected.Add(instance);
                else _injectedIdentities.RemoveAt(i);
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
