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
    private static readonly ThreadSafeDictionary<object, Task> _injectionTasks = new();

    /// <summary>
    /// Gets or creates an injection plan for the specified type.
    /// The plan builder will use source-generated metadata if available,
    /// otherwise falls back to reflection-based discovery.
    /// </summary>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
    #endif
    public static PropertyInjectionPlan GetOrCreatePlan(Type type)
    {
        return _injectionPlans.GetOrAdd(type, _ => PropertyInjectionPlanBuilder.Build(type));
    }

    /// <summary>
    /// Checks if a type has injectable properties using caching.
    /// </summary>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
    #endif
    public static bool HasInjectableProperties(Type type)
    {
        return _shouldInjectCache.GetOrAdd(type, t =>
        {
            var plan = GetOrCreatePlan(t);
            return plan.HasProperties;
        });
    }

    /// <summary>
    /// Gets or adds an injection task for the specified instance.
    /// </summary>
    public static async Task GetOrAddInjectionTask(object instance, Func<object, Task> taskFactory)
    {
        await _injectionTasks.GetOrAdd(instance, taskFactory);
    }

    /// <summary>
    /// Checks if an injection task already exists for the instance.
    /// </summary>
    public static bool TryGetInjectionTask(object instance, out Task? existingTask)
    {
        return _injectionTasks.TryGetValue(instance, out existingTask);
    }
}
