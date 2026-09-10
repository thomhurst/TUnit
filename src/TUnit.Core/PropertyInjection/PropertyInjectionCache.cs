using TUnit.Core.Data;

namespace TUnit.Core.PropertyInjection;

/// <summary>
/// Provides pure caching functionality for property injection metadata.
/// Follows Single Responsibility Principle - only caches type metadata, no execution logic.
///
/// This cache supports both execution modes:
/// - Source Generation Mode: Uses pre-compiled property setters and metadata
/// - Reflection Mode: Uses runtime discovery and dynamic property access
///
/// Instance-level injection tracking has been moved to ObjectLifecycleService
/// to maintain SRP (caching vs execution are separate concerns).
/// </summary>
internal static class PropertyInjectionCache
{
    private static readonly ThreadSafeDictionary<Type, PropertyInjectionPlan> _injectionPlans = new();
    private static readonly ThreadSafeDictionary<Type, bool> _shouldInjectCache = new();

    /// <summary>
    /// Gets or creates an injection plan for the specified type.
    /// The plan builder will use source-generated metadata if available,
    /// otherwise falls back to reflection-based discovery.
    /// </summary>
    public static PropertyInjectionPlan GetOrCreatePlan(Type type)
    {
        return _injectionPlans.GetOrAdd(type, PropertyInjectionPlanBuilder.Build);
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
}
