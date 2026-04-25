using System.ComponentModel;

namespace TUnit.Core.Hooks;

/// <summary>
/// Wraps a hook registration as a factory plus eagerly-computed registration index.
/// The full <see cref="HookMethod"/> graph (MethodMetadata, ClassMetadata, parameter arrays,
/// delegate, etc.) is constructed only on first call to <see cref="Materialize"/>.
/// This avoids paying O(N) construction cost per hook at module initialization.
/// <para>
/// The registration index is captured eagerly so that hook ordering is preserved even
/// when materialization happens out of declaration order.
/// </para>
/// </summary>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public sealed class LazyHookEntry<T> where T : HookMethod
{
    private Func<int, T>? _factory;
    private T? _materialized;
    private readonly object _lock = new();

    /// <summary>
    /// The eagerly-computed registration index. Used to preserve declaration order
    /// across hooks that share the same <see cref="HookMethod.Order"/> value.
    /// </summary>
    public int RegistrationIndex { get; }

    /// <summary>
    /// Creates a lazy entry from a factory that will produce the materialized hook on first
    /// access. The factory MUST be a static lambda (no captures) to avoid per-hook closure
    /// allocations and to remain AOT compatible. The eagerly-computed registration index is
    /// passed back into the factory so it can be assigned to <see cref="HookMethod.RegistrationIndex"/>.
    /// </summary>
    public LazyHookEntry(int registrationIndex, Func<int, T> factory)
    {
        RegistrationIndex = registrationIndex;
        _factory = factory;
    }

    /// <summary>
    /// Creates a lazy entry from an already-materialized hook (used by reflection-mode discovery).
    /// </summary>
    public LazyHookEntry(T hook)
    {
        _materialized = hook;
        RegistrationIndex = hook.RegistrationIndex;
    }

    /// <summary>
    /// Returns the materialized hook, constructing it on first access.
    /// Subsequent calls return the cached instance. Thread-safe.
    /// </summary>
    public T Materialize()
    {
        if (_materialized is not null)
        {
            return _materialized;
        }

        lock (_lock)
        {
            if (_materialized is not null)
            {
                return _materialized;
            }

            var hook = _factory!(RegistrationIndex);
            _materialized = hook;
            _factory = null;
            return hook;
        }
    }
}
