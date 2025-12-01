using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Core.Lifecycle;
using TUnit.Core.PropertyInjection;
using TUnit.Core.PropertyInjection.Initialization;

namespace TUnit.Engine.Services.Lifecycle;

/// <summary>
/// Consolidated implementation of object initialization for TUnit.
/// Replaces the fragmented initialization logic in:
/// - DataSourceInitializer
/// - PropertyInjectionService
/// - PropertyInitializationOrchestrator
/// - ObjectRegistrationService
///
/// This class coordinates with ObjectLifecycleManager for state tracking,
/// ensuring that all initialization operations are properly deduplicated.
/// </summary>
internal sealed class InitializationExecutor : IInitializationExecutor
{
    private readonly IObjectLifecycleManager _lifecycleManager;
    private readonly IObjectGraphWalker _graphWalker;
    private readonly IObjectRegistry? _objectRegistry;

    // Simple object pool for visited objects dictionaries to reduce allocations
    private static readonly ConcurrentBag<ConcurrentDictionary<object, byte>> _visitedObjectsPool = new();

    public InitializationExecutor(
        IObjectLifecycleManager lifecycleManager,
        IObjectGraphWalker graphWalker,
        IObjectRegistry? objectRegistry = null)
    {
        _lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
        _graphWalker = graphWalker ?? throw new ArgumentNullException(nameof(graphWalker));
        _objectRegistry = objectRegistry;
    }

    /// <inheritdoc />
    public async ValueTask<T> EnsureFullyInitializedAsync<T>(
        T instance,
        InitializationContext context,
        CancellationToken cancellationToken = default) where T : notnull
    {
        // Delegate to the lifecycle manager for state tracking and deduplication
        await _lifecycleManager.EnsureInitializedAsync(
            instance,
            context.ObjectBag,
            context.MethodMetadata,
            context.Events,
            context.PreResolvedValues,
            cancellationToken).ConfigureAwait(false);

        return instance;
    }

    /// <inheritdoc />
    public async ValueTask<T> EnsureRegisteredAsync<T>(
        T instance,
        InitializationContext context,
        CancellationToken cancellationToken = default) where T : notnull
    {
        // Registration phase: property injection only, no IAsyncInitializer
        await _lifecycleManager.EnsurePropertiesInjectedAsync(
            instance,
            context.ObjectBag,
            context.MethodMetadata,
            context.Events,
            context.PreResolvedValues,
            cancellationToken).ConfigureAwait(false);

        return instance;
    }

    /// <inheritdoc />
    public void TrackForDisposal(object instance)
    {
        _lifecycleManager.IncrementReferenceCount(instance);
    }

    /// <inheritdoc />
    public async ValueTask ReleaseAsync(object instance)
    {
        await _lifecycleManager.DecrementReferenceCountAsync(instance).ConfigureAwait(false);
    }

    /// <summary>
    /// Injects properties into an object using source-generated or reflection-based metadata.
    /// This method is called by the ObjectLifecycleManager via delegate.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access",
        Justification = "Property injection cache handles both AOT and reflection modes appropriately")]
    internal async ValueTask InjectPropertiesAsync(
        object instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents? events,
        IDictionary<string, object?>? preResolvedValues,
        CancellationToken cancellationToken)
    {
        if (instance == null)
        {
            return;
        }

        // Rent dictionary from pool to avoid allocations
        if (!_visitedObjectsPool.TryTake(out var visitedObjects))
        {
#if NETSTANDARD2_0
            visitedObjects = new ConcurrentDictionary<object, byte>();
#else
            visitedObjects = new ConcurrentDictionary<object, byte>(ReferenceEqualityComparer.Instance);
#endif
        }

        try
        {
            await InjectPropertiesIntoObjectCoreAsync(
                instance,
                objectBag,
                methodMetadata,
                events,
                visitedObjects,
                preResolvedValues).ConfigureAwait(false);
        }
        finally
        {
            // Clear and return to pool (reject if too large to avoid memory bloat)
            visitedObjects.Clear();
            if (visitedObjects.Count == 0)
            {
                _visitedObjectsPool.Add(visitedObjects);
            }
        }
    }

    private async Task InjectPropertiesIntoObjectCoreAsync(
        object instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents? events,
        ConcurrentDictionary<object, byte> visitedObjects,
        IDictionary<string, object?>? preResolvedValues)
    {
        // Prevent cycles
        if (!visitedObjects.TryAdd(instance, 0))
        {
            return;
        }

        var plan = PropertyInjectionCache.GetOrCreatePlan(instance.GetType());
        if (!plan.HasProperties)
        {
            return;
        }

        events ??= new TestContextEvents();

        try
        {
            await InitializeObjectWithPropertiesAsync(
                instance,
                plan,
                objectBag,
                methodMetadata,
                events,
                visitedObjects,
                preResolvedValues).ConfigureAwait(false);

            await RecurseIntoNestedPropertiesAsync(
                instance,
                objectBag,
                methodMetadata,
                events,
                visitedObjects).ConfigureAwait(false);
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

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access",
        Justification = "Specific source gen path")]
    private async Task InitializeObjectWithPropertiesAsync(
        object instance,
        PropertyInjectionPlan plan,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        IDictionary<string, object?>? preResolvedValues)
    {
        if (!plan.HasProperties)
        {
            return;
        }

        // Initialize properties based on what's available in the plan
        if (plan.SourceGeneratedProperties.Length > 0)
        {
            var tasks = plan.SourceGeneratedProperties.Select(metadata =>
                InitializeSourceGeneratedPropertyAsync(
                    instance,
                    metadata,
                    objectBag,
                    methodMetadata,
                    events,
                    visitedObjects,
                    preResolvedValues));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        else if (plan.ReflectionProperties.Length > 0)
        {
            var tasks = plan.ReflectionProperties.Select(pair =>
                InitializeReflectionPropertyAsync(
                    instance,
                    pair.Property,
                    pair.DataSource,
                    objectBag,
                    methodMetadata,
                    events,
                    visitedObjects,
                    preResolvedValues));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access",
        Justification = "Property data resolver handles AOT appropriately")]
    private async Task InitializeSourceGeneratedPropertyAsync(
        object instance,
        PropertyInjectionMetadata metadata,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        IDictionary<string, object?>? preResolvedValues)
    {
        object? resolvedValue = null;
        var testContext = TestContext.Current;

        // Check if property was pre-resolved during registration (explicit parameter takes precedence)
        if (preResolvedValues?.TryGetValue(metadata.PropertyName, out resolvedValue) == true && resolvedValue != null)
        {
            // Use pre-resolved value - it was already initialized during registration phase
        }
        else
        {
            // Resolve the property value from the data source
            resolvedValue = await ResolvePropertyDataAsync(
                new PropertyInitializationContext
                {
                    Instance = instance,
                    SourceGeneratedMetadata = metadata,
                    PropertyName = metadata.PropertyName,
                    PropertyType = metadata.PropertyType,
                    PropertySetter = metadata.SetProperty,
                    ObjectBag = objectBag,
                    MethodMetadata = methodMetadata,
                    Events = events,
                    VisitedObjects = visitedObjects,
                    TestContext = testContext,
                    IsNestedProperty = false
                }).ConfigureAwait(false);

            if (resolvedValue == null)
            {
                return;
            }
        }

        // Set the property value
        metadata.SetProperty(instance, resolvedValue);

        // Store for potential reuse
        if (testContext != null)
        {
            ((ConcurrentDictionary<string, object?>)testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments)
                .TryAdd(metadata.PropertyName, resolvedValue);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access",
        Justification = "Reflection-based property initialization is only used in reflection mode")]
    private async Task InitializeReflectionPropertyAsync(
        object instance,
        System.Reflection.PropertyInfo property,
        IDataSourceAttribute dataSource,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        IDictionary<string, object?>? preResolvedValues)
    {
        var testContext = TestContext.Current;
        var propertySetter = PropertySetterFactory.CreateSetter(property);

        // Check if property was pre-resolved during registration
        if (preResolvedValues?.TryGetValue(property.Name, out var preResolvedValue) == true && preResolvedValue != null)
        {
            propertySetter(instance, preResolvedValue);
            return;
        }

        // Resolve the property value from the data source
        var resolvedValue = await ResolvePropertyDataAsync(
            new PropertyInitializationContext
            {
                Instance = instance,
                PropertyInfo = property,
                DataSource = dataSource,
                PropertyName = property.Name,
                PropertyType = property.PropertyType,
                PropertySetter = propertySetter,
                ObjectBag = objectBag,
                MethodMetadata = methodMetadata,
                Events = events,
                VisitedObjects = visitedObjects,
                TestContext = testContext,
                IsNestedProperty = false
            }).ConfigureAwait(false);

        if (resolvedValue == null)
        {
            return;
        }

        propertySetter(instance, resolvedValue);
    }

    private async Task RecurseIntoNestedPropertiesAsync(
        object instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        var plan = PropertyInjectionCache.GetOrCreatePlan(instance.GetType());
        if (!plan.HasProperties)
        {
            return;
        }

        if (plan.SourceGeneratedProperties.Length > 0)
        {
            foreach (var metadata in plan.SourceGeneratedProperties)
            {
                var property = metadata.ContainingType.GetProperty(metadata.PropertyName);
                if (property == null || !property.CanRead)
                {
                    continue;
                }

                var propertyValue = property.GetValue(instance);
                if (propertyValue == null)
                {
                    continue;
                }

                if (PropertyInjectionCache.HasInjectableProperties(propertyValue.GetType()))
                {
                    await InjectPropertiesIntoObjectCoreAsync(
                        propertyValue,
                        objectBag,
                        methodMetadata,
                        events,
                        visitedObjects,
                        null).ConfigureAwait(false);
                }
            }
        }
        else if (plan.ReflectionProperties.Length > 0)
        {
            foreach (var (property, _) in plan.ReflectionProperties)
            {
                var propertyValue = property.GetValue(instance);
                if (propertyValue == null)
                {
                    continue;
                }

                if (PropertyInjectionCache.HasInjectableProperties(propertyValue.GetType()))
                {
                    await InjectPropertiesIntoObjectCoreAsync(
                        propertyValue,
                        objectBag,
                        methodMetadata,
                        events,
                        visitedObjects,
                        null).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    /// Resolves a property value from its data source.
    /// Simplified version of PropertyDataResolver that integrates with the lifecycle manager.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access",
        Justification = "Property data resolution uses reflection appropriately")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:'propertyType' argument does not satisfy 'DynamicallyAccessedMembersAttribute'",
        Justification = "Property types come from source generator or cached reflection metadata")]
    private async Task<object?> ResolvePropertyDataAsync(PropertyInitializationContext context)
    {
        IDataSourceAttribute? dataSource = null;

        if (context.DataSource != null)
        {
            dataSource = context.DataSource;
        }
        else if (context.SourceGeneratedMetadata != null)
        {
            dataSource = context.SourceGeneratedMetadata.CreateDataSource();
        }

        if (dataSource == null)
        {
            return null;
        }

        // Ensure the data source is initialized
        if (PropertyInjectionCache.HasInjectableProperties(dataSource.GetType()) || dataSource is IAsyncInitializer)
        {
            await _lifecycleManager.EnsureInitializedAsync(
                dataSource,
                context.ObjectBag,
                context.MethodMetadata,
                context.Events).ConfigureAwait(false);
        }

        var dataGeneratorMetadata = CreateDataGeneratorMetadata(context, dataSource);
        var dataRows = dataSource.GetDataRowsAsync(dataGeneratorMetadata);

        await foreach (var factory in dataRows)
        {
            var args = await factory().ConfigureAwait(false);
            var value = ResolveValueFromArgs(context.PropertyType, args);
            value = await ResolveDelegateValue(value).ConfigureAwait(false);

            if (value != null)
            {
                if (PropertyInjectionCache.HasInjectableProperties(value.GetType()) || value is IAsyncInitializer)
                {
                    await _lifecycleManager.EnsureInitializedAsync(
                        value,
                        context.ObjectBag,
                        context.MethodMetadata,
                        context.Events).ConfigureAwait(false);
                }

                return value;
            }
        }

        return null;
    }

    private static DataGeneratorMetadata CreateDataGeneratorMetadata(
        PropertyInitializationContext context,
        IDataSourceAttribute dataSource)
    {
        var testBuilderContext = new TestBuilderContext
        {
            TestMetadata = context.MethodMetadata!,
            DataSourceAttribute = dataSource,
            Events = context.Events,
            StateBag = context.ObjectBag
        };

        return new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(testBuilderContext),
            MembersToGenerate = [],
            TestInformation = context.MethodMetadata,
            Type = Core.Enums.DataGeneratorType.Property,
            TestSessionId = TestSessionContext.Current?.Id ?? "initialization",
            TestClassInstance = context.Instance,
            ClassInstanceArguments = context.TestContext?.Metadata.TestDetails.TestClassArguments ?? []
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute'",
        Justification = "ValueTuple types are well-known and their constructors are preserved by the runtime")]
    private static object? ResolveValueFromArgs(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type propertyType,
        object?[]? args)
    {
        if (args == null || args.Length == 0)
        {
            return null;
        }

        if (args.Length == 1)
        {
            return args[0];
        }

        // Handle tuple types
        if (propertyType.IsGenericType && propertyType.Name.StartsWith("ValueTuple`", StringComparison.Ordinal))
        {
            return Activator.CreateInstance(propertyType, args);
        }

        return args[0];
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute'",
        Justification = "Func<T>, Task<T>, and ValueTask<T> are well-known types with preserved methods")]
    private static async ValueTask<object?> ResolveDelegateValue(object? value)
    {
        while (value != null)
        {
            var valueType = value.GetType();

            if (valueType.IsGenericType)
            {
                var genericTypeDef = valueType.GetGenericTypeDefinition();

                if (genericTypeDef == typeof(Func<>))
                {
                    var invokeMethod = valueType.GetMethod("Invoke");
                    value = invokeMethod?.Invoke(value, null);
                    continue;
                }

                if (genericTypeDef == typeof(Task<>) || genericTypeDef == typeof(ValueTask<>))
                {
                    var awaitable = (dynamic)value;
                    value = await awaitable;
                    continue;
                }
            }

            break;
        }

        return value;
    }
}
