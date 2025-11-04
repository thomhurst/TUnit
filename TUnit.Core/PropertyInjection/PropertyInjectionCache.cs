using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Data;

namespace TUnit.Core.PropertyInjection;

/// <summary>
/// Provides caching functionality for property injection operations.
/// Follows Single Responsibility Principle by focusing only on caching.
///
/// This cache supports both execution modes:
/// - Source Generation Mode: Uses pre-compiled property setters and metadata
/// - Reflection Mode: Uses runtime discovery and dynamic property access
///
/// The IL2067 suppressions are necessary because types come from runtime objects
/// (via GetType() calls) which cannot have compile-time annotations.
/// </summary>
internal static class PropertyInjectionCache
{
    private static readonly ThreadSafeDictionary<Type, PropertyInjectionPlan> _injectionPlans = new();
    private static readonly ThreadSafeDictionary<Type, bool> _shouldInjectCache = new();
    private static readonly ThreadSafeDictionary<object, TaskCompletionSource> _injectionTasks = new();

    /// <summary>
    /// Gets or creates an injection plan for the specified type.
    /// The plan builder will use source-generated metadata if available,
    /// otherwise falls back to reflection-based discovery.
    /// </summary>
    public static PropertyInjectionPlan GetOrCreatePlan(Type type)
    {
        return _injectionPlans.GetOrAdd(type, _ => PropertyInjectionPlanBuilder.Build(type));
    }

    /// <summary>
    /// Checks if a type has injectable properties using caching.
    /// </summary>
    public static bool HasInjectableProperties(Type type)
    {
        return _shouldInjectCache.GetOrAdd(type, t =>
        {
            var plan = GetOrCreatePlan(t);
            return plan.HasProperties;
        });
    }

    /// <summary>
    /// Ensures properties are injected into the specified instance.
    /// Optimized with fast-path for already-injected instances (zero allocation).
    /// Uses TaskCompletionSource for lock-free coordination across threads.
    /// </summary>
    public static async ValueTask EnsureInjectedAsync(object instance, Func<object, ValueTask> injectionFactory)
    {
        // Fast path: Check if already injected (avoids Task allocation)
        if (_injectionTasks.TryGetValue(instance, out var existingTcs) && existingTcs.Task.IsCompleted)
        {
            // Already injected - return synchronously without allocating a Task
            if (existingTcs.Task.IsFaulted)
            {
                // Re-throw the original exception
                await existingTcs.Task.ConfigureAwait(false);
            }

            return;
        }

        // Slow path: Need to inject or wait for injection
        // Use TaskCompletionSource for lock-free, efficient injection coordination
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        existingTcs = _injectionTasks.GetOrAdd(instance, _ => tcs);

        if (existingTcs == tcs)
        {
            // We won the race - this thread is responsible for injection
            try
            {
                await injectionFactory(instance).ConfigureAwait(false);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                throw;
            }
        }
        else
        {
            // Another thread is injecting or already injected - wait for it
            await existingTcs.Task.ConfigureAwait(false);
        }
    }
}
