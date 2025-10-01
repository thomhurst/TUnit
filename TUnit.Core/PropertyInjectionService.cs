using TUnit.Core;
using TUnit.Core.Tracking;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Data;
using TUnit.Core.DataSources;
using TUnit.Core.Initialization;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Core.Enums;
using TUnit.Core.Services;
using TUnit.Core.Helpers;
using TUnit.Core.PropertyInjection;
using TUnit.Core.PropertyInjection.Initialization;
using System.Reflection;
using System.Collections.Concurrent;

namespace TUnit.Core;

/// <summary>
/// Internal service for property injection.
/// Used by ObjectRegistrationService during registration phase.
/// </summary>
internal sealed class PropertyInjectionService
{
    private readonly PropertyInitializationOrchestrator _orchestrator;

    public PropertyInjectionService(DataSourceInitializer dataSourceInitializer)
    {
        // We'll set ObjectRegistrationService later to break the circular dependency
        _orchestrator = new PropertyInitializationOrchestrator(dataSourceInitializer, null!);
    }
    
    public void Initialize(ObjectRegistrationService objectRegistrationService)
    {
        _orchestrator.Initialize(objectRegistrationService);
    }

    /// <summary>
    /// Injects properties with data sources into argument objects just before test execution.
    /// This ensures properties are only initialized when the test is about to run.
    /// Arguments are processed in parallel for better performance.
    /// </summary>
    public async Task InjectPropertiesIntoArgumentsAsync(object?[] arguments, Dictionary<string, object?> objectBag, MethodMetadata methodMetadata, TestContextEvents events)
    {
        if (arguments.Length == 0)
        {
            return;
        }

        // Fast path: check if any arguments need injection
        var injectableArgs = arguments
            .Where(argument => argument != null && PropertyInjectionCache.HasInjectableProperties(argument.GetType()))
            .ToArray();
            
        if (injectableArgs.Length == 0)
        {
            return;
        }

        // Process arguments in parallel
        var argumentTasks = injectableArgs
            .Select(argument => InjectPropertiesIntoObjectAsync(argument!, objectBag, methodMetadata, events))
            .ToArray();

        await Task.WhenAll(argumentTasks);
    }


    /// <summary>
    /// Recursively injects properties with data sources into a single object.
    /// Uses source generation mode when available, falls back to reflection mode.
    /// After injection, handles tracking, initialization, and recursive injection.
    /// </summary>
    public Task InjectPropertiesIntoObjectAsync(object instance, Dictionary<string, object?>? objectBag, MethodMetadata? methodMetadata, TestContextEvents? events)
    {
        // Start with an empty visited set for cycle detection
#if NETSTANDARD2_0
        var visitedObjects = new ConcurrentDictionary<object, byte>();
#else
        var visitedObjects = new ConcurrentDictionary<object, byte>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
#endif
        return InjectPropertiesIntoObjectAsyncCore(instance, objectBag, methodMetadata, events, visitedObjects);
    }
    
    internal async Task InjectPropertiesIntoObjectAsyncCore(object instance, Dictionary<string, object?>? objectBag, MethodMetadata? methodMetadata, TestContextEvents? events, ConcurrentDictionary<object, byte> visitedObjects)
    {
        if (instance == null)
        {
            return;
        }


        // Prevent cycles - if we're already processing this object, skip it
        // TryAdd returns false if the key already exists (thread-safe)
        if (!visitedObjects.TryAdd(instance, 0))
        {
            return;
        }

        // If we don't have the required context, try to get it from the current test context
        objectBag ??= TestContext.Current?.ObjectBag ?? new Dictionary<string, object?>();
        methodMetadata ??= TestContext.Current?.TestDetails?.MethodMetadata;
        events ??= TestContext.Current?.Events;

        // If we still don't have events after trying to get from context, create a default instance
        events ??= new TestContextEvents();

        try
        {
            var alreadyProcessed = PropertyInjectionCache.TryGetInjectionTask(instance, out var existingTask);
            
            if (alreadyProcessed && existingTask != null)
            {
                await existingTask;

                var plan = PropertyInjectionCache.GetOrCreatePlan(instance.GetType());
                if (plan.HasProperties)
                {
                    if (SourceRegistrar.IsEnabled)
                    {
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
                                    if (PropertyInjectionCache.HasInjectableProperties(propertyValue.GetType()))
                                    {
                                        await InjectPropertiesIntoObjectAsyncCore(propertyValue, objectBag, methodMetadata, events, visitedObjects);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var (property, _) in plan.ReflectionProperties)
                        {
                            var propertyValue = property.GetValue(instance);
                            if (propertyValue != null)
                            {
                                if (PropertyInjectionCache.HasInjectableProperties(propertyValue.GetType()))
                                {
                                    await InjectPropertiesIntoObjectAsyncCore(propertyValue, objectBag, methodMetadata, events, visitedObjects);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                await PropertyInjectionCache.GetOrAddInjectionTask(instance, async _ =>
                {
                    var plan = PropertyInjectionCache.GetOrCreatePlan(instance.GetType());

                    // Use the new orchestrator for property initialization
                    await _orchestrator.InitializeObjectWithPropertiesAsync(
                        instance, plan, objectBag, methodMetadata, events, visitedObjects);
                });

                // After orchestrator completes, recursively inject nested properties
                var plan = PropertyInjectionCache.GetOrCreatePlan(instance.GetType());
                if (plan.HasProperties)
                {
                    if (SourceRegistrar.IsEnabled)
                    {
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
                                    if (PropertyInjectionCache.HasInjectableProperties(propertyValue.GetType()))
                                    {
                                        await InjectPropertiesIntoObjectAsyncCore(propertyValue, objectBag, methodMetadata, events, visitedObjects);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var (property, _) in plan.ReflectionProperties)
                        {
                            var propertyValue = property.GetValue(instance);
                            if (propertyValue != null)
                            {
                                if (PropertyInjectionCache.HasInjectableProperties(propertyValue.GetType()))
                                {
                                    await InjectPropertiesIntoObjectAsyncCore(propertyValue, objectBag, methodMetadata, events, visitedObjects);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var detailedMessage = $"Failed to inject properties for type '{instance.GetType().Name}': {ex.Message}";
            if (ex.StackTrace != null)
            {
                detailedMessage += $"\nStack trace: {ex.StackTrace}";
            }
            throw new InvalidOperationException(detailedMessage, ex);
        }
    }




}
