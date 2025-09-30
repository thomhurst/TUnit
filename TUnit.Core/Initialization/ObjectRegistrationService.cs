using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Core.DataSources;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;

namespace TUnit.Core.Initialization;

/// <summary>
/// Handles object registration during the test discovery/registration phase.
/// Responsibilities: Create instances, inject properties, track for disposal (ONCE per object).
/// Does NOT call IAsyncInitializer - that's deferred to ObjectInitializationService during execution.
/// </summary>
internal sealed class ObjectRegistrationService
{
    private readonly PropertyInjectionService _propertyInjectionService;
    private readonly DataSourceInitializer _dataSourceInitializer;

    public ObjectRegistrationService(
        PropertyInjectionService propertyInjectionService,
        DataSourceInitializer dataSourceInitializer)
    {
        _propertyInjectionService = propertyInjectionService ?? throw new ArgumentNullException(nameof(propertyInjectionService));
        _dataSourceInitializer = dataSourceInitializer ?? throw new ArgumentNullException(nameof(dataSourceInitializer));
    }

    /// <summary>
    /// Registers a single object during the registration phase.
    /// Injects properties, tracks for disposal (once), but does NOT call IAsyncInitializer.
    /// </summary>
    public async Task RegisterObjectAsync(
        object instance,
        Dictionary<string, object?>? objectBag = null,
        MethodMetadata? methodMetadata = null,
        TestContextEvents? events = null)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        // Prepare context
        objectBag ??= TestContext.Current?.ObjectBag ?? new Dictionary<string, object?>();
        methodMetadata ??= TestContext.Current?.TestDetails?.MethodMetadata;
        events ??= TestContext.Current?.Events ?? new TestContextEvents();

        // Step 1: Property injection (may create nested instances)
        if (RequiresPropertyInjection(instance))
        {
            await _propertyInjectionService.InjectPropertiesIntoObjectAsync(
                instance,
                objectBag,
                methodMetadata,
                events);
        }

        // Step 2: Track for disposal (ONCE - PropertyInjectionService no longer tracks)
        TrackObject(instance, events);

        // Step 3: NO IAsyncInitializer calls - deferred to execution phase
    }

    /// <summary>
    /// Registers multiple objects (e.g., constructor/method arguments) in parallel.
    /// Used during test registration to prepare arguments without executing expensive operations.
    /// </summary>
    public async Task RegisterArgumentsAsync(
        object?[] arguments,
        Dictionary<string, object?> objectBag,
        MethodMetadata methodMetadata,
        TestContextEvents events)
    {
        if (arguments == null || arguments.Length == 0)
        {
            return;
        }

        // Process arguments in parallel for performance
        var tasks = new List<Task>();
        foreach (var argument in arguments)
        {
            if (argument != null)
            {
                tasks.Add(RegisterObjectAsync(argument, objectBag, methodMetadata, events));
            }
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Registers a test class instance during test discovery.
    /// Replaces TestObjectInitializer.InitializeTestClassAsync for the registration phase.
    /// </summary>
    public async Task RegisterTestClassAsync(
        object testClassInstance,
        TestContext testContext)
    {
        if (testClassInstance == null)
        {
            throw new ArgumentNullException(nameof(testClassInstance));
        }

        // Track the test class instance first
        TrackObject(testClassInstance, testContext.Events);

        // Register the instance (property injection only, no IAsyncInitializer)
        await RegisterObjectAsync(
            testClassInstance,
            testContext.ObjectBag,
            testContext.TestDetails?.MethodMetadata,
            testContext.Events);
    }

    /// <summary>
    /// Determines if an object requires property injection.
    /// </summary>
    private bool RequiresPropertyInjection(object instance)
    {
        return PropertyInjection.PropertyInjectionCache.HasInjectableProperties(instance.GetType());
    }

    /// <summary>
    /// Tracks an object for disposal using idempotent tracker.
    /// Multiple calls with the same object are safe - first succeeds, subsequent are no-ops.
    /// </summary>
    private void TrackObject(object instance, TestContextEvents events)
    {
        if (events != null)
        {
            ObjectLifecycleTracker.TrackObjectForDisposal(events, instance);
        }
    }
}