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
    private static readonly ThreadSafeDictionary<object, TaskCompletionSource<bool>> _injectionTasks = new();

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
    /// Fast-path optimized for already-injected instances (zero allocation).
    /// </summary>
    public static async ValueTask EnsureInjectedAsync(object instance, Func<object, ValueTask> injectionFactory)
    {
        if (_injectionTasks.TryGetValue(instance, out var existingTcs) && existingTcs.Task.IsCompleted)
        {
            if (existingTcs.Task.IsFaulted)
            {
                await existingTcs.Task.ConfigureAwait(false);
            }

            return;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        existingTcs = _injectionTasks.GetOrAdd(instance, _ => tcs);

        if (existingTcs == tcs)
        {
            try
            {
                await injectionFactory(instance).ConfigureAwait(false);
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                throw;
            }
        }
        else
        {
            await existingTcs.Task.ConfigureAwait(false);
        }
    }
}
