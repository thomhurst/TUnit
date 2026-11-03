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

        lock (_lock)
        {
            if (!_objects.TryGetValue(instance, out var owners))
                _objects.Add(instance, owners = []);
            if (!owners.Contains(owner))
                owners.Add(owner);
        }
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
    internal sealed class ScopeOwner(object? scope)
    {
        private int _disposed;

        internal async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            if (scope is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (scope is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
