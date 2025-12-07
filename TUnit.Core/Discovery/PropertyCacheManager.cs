using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Discovery;

/// <summary>
/// Manages cached property reflection results for object graph discovery.
/// Extracted from ObjectGraphDiscoverer to follow Single Responsibility Principle.
/// </summary>
/// <remarks>
/// <para>
/// This class caches <see cref="PropertyInfo"/> arrays per type to avoid repeated reflection calls.
/// Includes automatic cache cleanup when size exceeds <see cref="MaxCacheSize"/> to prevent memory leaks.
/// </para>
/// <para>
/// Thread-safe: Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> and <see cref="Interlocked"/> for coordination.
/// </para>
/// </remarks>
internal static class PropertyCacheManager
{
    /// <summary>
    /// Maximum size for the property cache before cleanup is triggered.
    /// Prevents unbounded memory growth in long-running test sessions.
    /// </summary>
    private const int MaxCacheSize = 10000;

    // Cache for GetProperties() results per type - eliminates repeated reflection calls
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    // Flag to coordinate cache cleanup (prevents multiple threads cleaning simultaneously)
    private static int _cleanupInProgress;

    /// <summary>
    /// Gets cached properties for a type, filtering to only readable non-indexed properties.
    /// Includes periodic cache cleanup to prevent unbounded memory growth.
    /// </summary>
    /// <param name="type">The type to get properties for.</param>
    /// <returns>An array of readable, non-indexed properties for the type.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection fallback for nested initializers. In AOT, source-gen handles primary discovery.")]
    public static PropertyInfo[] GetCachedProperties(Type type)
    {
        // Periodic cleanup if cache grows too large to prevent memory leaks
        // Use Interlocked to ensure only one thread performs cleanup at a time
        if (PropertyCache.Count > MaxCacheSize &&
            Interlocked.CompareExchange(ref _cleanupInProgress, 1, 0) == 0)
        {
            try
            {
                // Double-check after acquiring cleanup flag
                if (PropertyCache.Count > MaxCacheSize)
                {
                    var keysToRemove = new List<Type>(MaxCacheSize / 2);
                    var count = 0;
                    foreach (var key in PropertyCache.Keys)
                    {
                        if (count++ >= MaxCacheSize / 2)
                        {
                            break;
                        }

                        keysToRemove.Add(key);
                    }

                    foreach (var key in keysToRemove)
                    {
                        PropertyCache.TryRemove(key, out _);
                    }
#if DEBUG
                    Debug.WriteLine($"[PropertyCacheManager] PropertyCache exceeded {MaxCacheSize} entries, cleared {keysToRemove.Count} entries");
#endif
                }
            }
            finally
            {
                Interlocked.Exchange(ref _cleanupInProgress, 0);
            }
        }

        return PropertyCache.GetOrAdd(type, static t =>
        {
            // Use explicit loops instead of LINQ to avoid allocations in hot path
            var allProps = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            // First pass: count eligible properties
            var eligibleCount = 0;
            foreach (var p in allProps)
            {
                if (p.CanRead && p.GetIndexParameters().Length == 0)
                {
                    eligibleCount++;
                }
            }

            // Second pass: fill result array
            var result = new PropertyInfo[eligibleCount];
            var i = 0;
            foreach (var p in allProps)
            {
                if (p.CanRead && p.GetIndexParameters().Length == 0)
                {
                    result[i++] = p;
                }
            }

            return result;
        });
    }

    /// <summary>
    /// Clears the property cache. Called at end of test session to release memory.
    /// </summary>
    public static void ClearCache()
    {
        PropertyCache.Clear();
    }

    /// <summary>
    /// Gets the current number of cached types. Useful for diagnostics.
    /// </summary>
    public static int CacheCount => PropertyCache.Count;
}
