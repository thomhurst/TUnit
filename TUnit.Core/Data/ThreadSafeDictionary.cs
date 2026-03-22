using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Data;

/// <summary>
/// Provides a thread-safe dictionary with lazy value initialization.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary. Must be non-null.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <remarks>
/// This class provides a concurrent dictionary where values are lazily initialized on first access.
/// Each value is created exactly once per key, even when accessed concurrently from multiple threads.
/// </remarks>
#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class ThreadSafeDictionary<TKey, TValue>
    where TKey : notnull
{
    // Inlines factory + args to avoid the separate closure that Lazy<T> requires.
    // Lazy<T> with ExecutionAndPublication costs 3 heap objects per new key:
    //   Lazy<T> + LazyHelper (internal lock object) + closure capturing factory args.
    // These custom subclasses reduce that to 1 allocation per new key.
    private abstract class LazyValue
    {
        private TValue? _value;
        private volatile int _initialized;

        protected abstract TValue Create();

        public TValue Value
        {
            get
            {
                if (_initialized == 1)
                {
                    return _value!;
                }

                lock (this)
                {
                    if (_initialized == 0)
                    {
                        _value = Create();
                        _initialized = 1;
                    }
                }

                return _value!;
            }
        }

        public bool IsValueCreated => _initialized == 1;
    }

    private sealed class LazyValueFromFunc : LazyValue
    {
        private Func<TKey, TValue>? _factory;
        private TKey _key;

        public LazyValueFromFunc(TKey key, Func<TKey, TValue> factory)
        {
            _key = key;
            _factory = factory;
        }

        protected override TValue Create()
        {
            var result = _factory!(_key);
            _factory = null; // allow factory closure to be GC'd after initialization
            _key = default!;
            return result;
        }
    }

    private sealed class LazyValueWithArg<TArg> : LazyValue
    {
        private Func<TKey, TArg, TValue>? _factory;
        private TKey _key;
        private TArg _arg;

        public LazyValueWithArg(TKey key, Func<TKey, TArg, TValue> factory, TArg arg)
        {
            _key = key;
            _factory = factory;
            _arg = arg;
        }

        protected override TValue Create()
        {
            var result = _factory!(_key, _arg);
            _factory = null; // allow factory closure to be GC'd after initialization
            _key = default!;
            _arg = default!;
            return result;
        }
    }

    private readonly ConcurrentDictionary<TKey, LazyValue> _innerDictionary = new();

    /// <summary>
    /// Gets a collection containing the keys in the dictionary.
    /// </summary>
    public ICollection<TKey> Keys => _innerDictionary.Keys;

    /// <summary>
    /// Gets an enumerable collection of values in the dictionary, forcing initialization of any uninitialized entries.
    /// </summary>
    public IEnumerable<TValue> Values => _innerDictionary.Values.Select(static lv => lv.Value);

    /// <summary>
    /// Gets the value associated with the specified key, or creates it if it doesn't exist.
    /// The factory is guaranteed to run at most once per key even under concurrent access.
    /// </summary>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func)
    {
        if (_innerDictionary.TryGetValue(key, out var existingLazy))
        {
            return existingLazy.Value;
        }

        var winning = _innerDictionary.GetOrAdd(key,
            static (k, f) => new LazyValueFromFunc(k, f),
            func);
        return winning.Value;
    }

    /// <summary>
    /// Gets the value associated with the specified key, or creates it using the factory and arg if it doesn't exist.
    /// Avoids closure allocation by accepting the factory argument explicitly.
    /// </summary>
    public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> func, TArg arg)
    {
        if (_innerDictionary.TryGetValue(key, out var existingLazy))
        {
            return existingLazy.Value;
        }

        var winning = _innerDictionary.GetOrAdd(key,
            static (k, state) => new LazyValueWithArg<TArg>(k, state.func, state.arg),
            (func, arg));
        return winning.Value;
    }

    /// <summary>
    /// Tries to get the value associated with the specified key.
    /// </summary>
    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        if (_innerDictionary.TryGetValue(key, out var lazy))
        {
            value = lazy.Value!;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Removes the value with the specified key from the dictionary.
    /// Returns the value only if it had already been initialized; otherwise returns default.
    /// </summary>
    public TValue? Remove(TKey key)
    {
        if (_innerDictionary.TryRemove(key, out var lazy) && lazy.IsValueCreated)
        {
            return lazy.Value;
        }

        return default;
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the specified key is not found.</exception>
    public TValue this[TKey key] => _innerDictionary.TryGetValue(key, out var lazy)
        ? lazy.Value
        : throw new KeyNotFoundException($"Key '{key}' not found in dictionary");
}
