namespace TUnit.Core;

/// <summary>
/// Owns a row's scopes. Object lookup releases each scope at its first resolved object's graph
/// depth; completion releases scopes left by failed factories or untracked values.
/// </summary>
internal sealed class DataRowScopeCollection
{
    private readonly List<ScopeOwner> _owners = [];
    private readonly Dictionary<object, List<ScopeOwner>> _objects = new(ReferenceEqualityComparer.Instance);

    internal ScopeOwner Register(object? scope)
    {
        var owner = new ScopeOwner(scope);
        _owners.Add(owner);
        return owner;
    }

    internal void RegisterObject(ScopeOwner owner, object? instance)
    {
        if (instance is null)
        {
            return;
        }

        if (!_objects.TryGetValue(instance, out var owners))
        {
            _objects.Add(instance, owners = []);
        }

        if (!owners.Contains(owner))
        {
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
        _owners.AddRange(scopes._owners);
        foreach (var pair in scopes._objects)
        {
            foreach (var owner in pair.Value)
            {
                RegisterObject(owner, pair.Key);
            }
        }
    }

    internal ValueTask<List<Exception>?> ReleaseAsync(object instance, List<Exception>? exceptions = null)
        => _objects.TryGetValue(instance, out var owners)
            ? DisposeAsync(owners, exceptions)
            : new(exceptions);

    internal ValueTask<List<Exception>?> CompleteAsync(List<Exception>? exceptions = null)
        => DisposeAsync(_owners, exceptions);

    private static async ValueTask<List<Exception>?> DisposeAsync(List<ScopeOwner> owners, List<Exception>? exceptions)
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
