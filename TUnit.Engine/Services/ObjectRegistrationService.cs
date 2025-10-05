using TUnit.Core;
using TUnit.Core.PropertyInjection;
using TUnit.Core.Tracking;

namespace TUnit.Engine.Services;

/// <summary>
/// Handles object registration during the test discovery/registration phase.
/// Responsibilities: Create instances, inject properties, track for disposal (ONCE per object).
/// Does NOT call IAsyncInitializer - that's deferred to ObjectInitializationService during execution.
/// </summary>
internal sealed class ObjectRegistrationService
{
    private readonly PropertyInjectionService _propertyInjectionService;

    public ObjectRegistrationService(
        PropertyInjectionService propertyInjectionService)
    {
        _propertyInjectionService = propertyInjectionService ?? throw new ArgumentNullException(nameof(propertyInjectionService));
    }

    /// <summary>
    /// Registers a single object during the registration phase.
    /// Injects properties, tracks for disposal (once), but does NOT call IAsyncInitializer.
    /// </summary>
    /// <param name="instance">The object instance to register. Must not be null.</param>
    /// <param name="objectBag">Shared object bag for the test context. Must not be null.</param>
    /// <param name="methodMetadata">Method metadata for the test. Can be null.</param>
    /// <param name="events">Test context events for tracking. Must not be null and must be unique per test permutation.</param>
    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
    #endif
    public async Task RegisterObjectAsync(
        object instance,
        Dictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (objectBag == null)
        {
            throw new ArgumentNullException(nameof(objectBag));
        }

        if (events == null)
        {
            throw new ArgumentNullException(nameof(events), "TestContextEvents must not be null. Each test permutation must have a unique TestContextEvents instance for proper disposal tracking.");
        }

        if (RequiresPropertyInjection(instance))
        {
            await _propertyInjectionService.InjectPropertiesIntoObjectAsync(
                instance,
                objectBag,
                methodMetadata,
                events);
        }
    }

    /// <summary>
    /// Registers multiple objects (e.g., constructor/method arguments) in parallel.
    /// Used during test registration to prepare arguments without executing expensive operations.
    /// </summary>
    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
    #endif
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
    /// Determines if an object requires property injection.
    /// </summary>
    private bool RequiresPropertyInjection(object instance)
    {
        return PropertyInjectionCache.HasInjectableProperties(instance.GetType());
    }
}
