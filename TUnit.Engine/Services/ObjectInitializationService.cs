using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.PropertyInjection;

namespace TUnit.Engine.Services;

/// <summary>
/// Handles object initialization during the test execution phase.
/// Responsibility: Call IAsyncInitializer.InitializeAsync() ONLY.
/// Does NOT inject properties or track objects - that's already done during registration.
/// </summary>
internal sealed class ObjectInitializationService
{
    /// <summary>
    /// Initializes an object by calling IAsyncInitializer.InitializeAsync() if implemented.
    /// This is called during test execution, after registration phase is complete.
    /// Recursively initializes all nested properties that implement IAsyncInitializer.
    /// </summary>
    public async Task InitializeAsync(object instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

#if NETSTANDARD2_0
        var visitedObjects = new HashSet<object>(new ReferenceEqualityComparer());
#else
        var visitedObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
#endif
        await InitializeAsyncCore(instance, visitedObjects);
    }

    /// <summary>
    /// Core initialization logic with cycle detection.
    /// Recursively initializes an object and all its nested properties.
    /// IMPORTANT: Nested properties are initialized FIRST (depth-first),
    /// so they are ready before the parent's InitializeAsync is called.
    /// </summary>
    private async Task InitializeAsyncCore(object instance, HashSet<object> visitedObjects)
    {
        if (instance == null || !visitedObjects.Add(instance))
        {
            return; // Already visited or null - prevent cycles
        }

        // First, recursively initialize nested properties (depth-first)
        // This ensures dependencies are ready before parent initialization
        await InitializeNestedPropertiesAsync(instance, visitedObjects);

        // Then call IAsyncInitializer on current instance
        // At this point, all nested properties are fully initialized
        if (instance is IAsyncInitializer asyncInitializer)
        {
            await asyncInitializer.InitializeAsync();
        }
    }

    /// <summary>
    /// Recursively initializes nested property values IN PARALLEL.
    /// This ensures deeply nested objects are also initialized during execution phase,
    /// and properties at the same level are initialized concurrently for performance.
    /// </summary>
    private async Task InitializeNestedPropertiesAsync(object instance, HashSet<object> visitedObjects)
    {
        var plan = PropertyInjectionCache.GetOrCreatePlan(instance.GetType());
        if (!plan.HasProperties)
        {
            return;
        }

        var initializationTasks = new List<Task>();

        if (SourceRegistrar.IsEnabled)
        {
            // Source-generated mode - initialize properties in parallel
            foreach (var metadata in plan.SourceGeneratedProperties)
            {
                if (metadata.ContainingType == null)
                {
                    continue;
                }

                var property = metadata.ContainingType.GetProperty(metadata.PropertyName);
                if (property != null && property.CanRead)
                {
                    var propertyValue = property.GetValue(instance);
                    if (propertyValue != null)
                    {
                        // Recursively initialize nested property and its children (in parallel)
                        initializationTasks.Add(InitializeAsyncCore(propertyValue, visitedObjects));
                    }
                }
            }
        }
        else
        {
            // Reflection mode - initialize properties in parallel
            foreach (var (property, _) in plan.ReflectionProperties)
            {
                var propertyValue = property.GetValue(instance);
                if (propertyValue != null)
                {
                    // Recursively initialize nested property and its children (in parallel)
                    initializationTasks.Add(InitializeAsyncCore(propertyValue, visitedObjects));
                }
            }
        }

        // Wait for all property initializations to complete
        if (initializationTasks.Count > 0)
        {
            await Task.WhenAll(initializationTasks);
        }
    }

#if NETSTANDARD2_0
    /// <summary>
    /// Reference equality comparer for .NET Standard 2.0 compatibility.
    /// </summary>
    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
#endif
}