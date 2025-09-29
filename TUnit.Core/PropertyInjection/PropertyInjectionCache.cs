using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using TUnit.Core.Data;

namespace TUnit.Core.PropertyInjection;

/// <summary>
/// Provides caching functionality for property injection operations.
/// Follows Single Responsibility Principle by focusing only on caching.
/// </summary>
internal static class PropertyInjectionCache
{
    private static readonly ThreadSafeDictionary<Type, PropertyInjectionPlan> _injectionPlans = new();
    private static readonly ThreadSafeDictionary<Type, bool> _shouldInjectCache = new();
    private static readonly ThreadSafeDictionary<object, Task> _injectionTasks = new();

    /// <summary>
    /// Gets or creates an injection plan for the specified type.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Type comes from runtime objects that cannot be annotated")]
    public static PropertyInjectionPlan GetOrCreatePlan(Type type)
    {
        return _injectionPlans.GetOrAdd(type, _ => PropertyInjectionPlanBuilder.Build(type));
    }

    /// <summary>
    /// Checks if a type has injectable properties using caching.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Type comes from runtime objects that cannot be annotated")]
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