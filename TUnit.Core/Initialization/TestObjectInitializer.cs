using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;

namespace TUnit.Core.Initialization;

/// <summary>
/// Centralized service for initializing test-related objects.
/// Provides a single entry point for the complete object initialization lifecycle.
/// </summary>
internal sealed class TestObjectInitializer
{
    private readonly PropertyInjectionService _propertyInjectionService;

    public TestObjectInitializer(PropertyInjectionService propertyInjectionService)
    {
        _propertyInjectionService = propertyInjectionService ?? throw new ArgumentNullException(nameof(propertyInjectionService));
    }

    /// <summary>
    /// Initializes a single object with the complete lifecycle:
    /// Create → Inject Properties → Initialize → Track → Ready
    /// </summary>
    public async Task<T> InitializeAsync<T>(
        T instance,
        TestContext? testContext = null) where T : notnull
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var context = PrepareContext(testContext);
        await InitializeObjectAsync(instance, context);
        return instance;
    }

    /// <summary>
    /// Initializes a single object with explicit context parameters.
    /// </summary>
    public async Task InitializeAsync(
        object instance,
        Dictionary<string, object?>? objectBag = null,
        MethodMetadata? methodMetadata = null,
        TestContextEvents? events = null)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var context = new InitializationContext
        {
            ObjectBag = objectBag ?? new Dictionary<string, object?>(),
            MethodMetadata = methodMetadata,
            Events = events ?? new TestContextEvents(),
            TestContext = TestContext.Current
        };

        await InitializeObjectAsync(instance, context);
    }

    /// <summary>
    /// Initializes multiple objects (e.g., test arguments) in parallel.
    /// </summary>
    public async Task InitializeArgumentsAsync(
        object?[] arguments,
        Dictionary<string, object?> objectBag,
        MethodMetadata methodMetadata,
        TestContextEvents events,
        bool isRegistrationPhase = false)
    {
        if (arguments == null || arguments.Length == 0)
        {
            return;
        }

        var context = new InitializationContext
        {
            ObjectBag = objectBag,
            MethodMetadata = methodMetadata,
            Events = events,
            TestContext = TestContext.Current,
            IsRegistrationPhase = isRegistrationPhase
        };

        // Process arguments in parallel for performance
        var tasks = new List<Task>();
        foreach (var argument in arguments)
        {
            if (argument != null)
            {
                tasks.Add(InitializeObjectAsync(argument, context));
            }
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Initializes test class instance with full lifecycle.
    /// </summary>
    public async Task InitializeTestClassAsync(
        object testClassInstance,
        TestContext testContext)
    {
        if (testClassInstance == null)
        {
            throw new ArgumentNullException(nameof(testClassInstance));
        }

        var context = PrepareContext(testContext);
        
        // Track the test class instance
        ObjectTracker.TrackObject(context.Events, testClassInstance);
        
        // Initialize the instance
        await InitializeObjectAsync(testClassInstance, context);
    }

    /// <summary>
    /// Core initialization logic - the single place where all initialization happens.
    /// </summary>
    private async Task InitializeObjectAsync(object instance, InitializationContext context)
    {
        try
        {
            // Step 1: Property Injection
            if (RequiresPropertyInjection(instance))
            {
                await _propertyInjectionService.InjectPropertiesIntoObjectAsync(
                    instance,
                    context.ObjectBag,
                    context.MethodMetadata,
                    context.Events);
            }

            // Step 2: Object Initialization (IAsyncInitializer)
            if (instance is IAsyncInitializer asyncInitializer)
            {
                // During registration phase, only initialize data source attributes.
                // Other IAsyncInitializer objects are deferred until test execution.
                if (!context.IsRegistrationPhase || instance is IDataSourceAttribute)
                {
                    await ObjectInitializer.InitializeAsync(instance);
                }
            }

            // Step 3: Tracking (if not already tracked)
            TrackObject(instance, context);

            // Step 4: Post-initialization hooks (future extension point)
            await OnObjectInitializedAsync(instance, context);
        }
        catch (Exception ex)
        {
            throw new TestObjectInitializationException(
                $"Failed to initialize object of type '{instance.GetType().Name}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Determines if an object requires property injection.
    /// </summary>
    private bool RequiresPropertyInjection(object instance)
    {
        // Use the existing cache from PropertyInjectionCache
        return PropertyInjection.PropertyInjectionCache.HasInjectableProperties(instance.GetType());
    }

    /// <summary>
    /// Tracks an object for disposal and ownership.
    /// </summary>
    private void TrackObject(object instance, InitializationContext context)
    {
        // Only track if we have events context
        if (context.Events != null)
        {
            ObjectTracker.TrackObject(context.Events, instance);
        }
    }

    /// <summary>
    /// Hook for post-initialization processing.
    /// </summary>
    private Task OnObjectInitializedAsync(object instance, InitializationContext context)
    {
        // Extension point for future features (e.g., validation, logging)
        return Task.CompletedTask;
    }

    /// <summary>
    /// Prepares initialization context from test context.
    /// </summary>
    private InitializationContext PrepareContext(TestContext? testContext)
    {
        return new InitializationContext
        {
            ObjectBag = testContext?.ObjectBag ?? new Dictionary<string, object?>(),
            MethodMetadata = testContext?.TestDetails?.MethodMetadata,
            Events = testContext?.Events ?? new TestContextEvents(),
            TestContext = testContext
        };
    }

    /// <summary>
    /// Internal context for initialization.
    /// </summary>
    private class InitializationContext
    {
        public Dictionary<string, object?> ObjectBag { get; set; } = null!;
        public MethodMetadata? MethodMetadata { get; set; }
        public TestContextEvents Events { get; set; } = null!;
        public TestContext? TestContext { get; set; }
        public bool IsRegistrationPhase { get; set; }
    }
}

/// <summary>
/// Exception thrown when test object initialization fails.
/// </summary>
public class TestObjectInitializationException : Exception
{
    public TestObjectInitializationException(string message) : base(message)
    {
    }

    public TestObjectInitializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}