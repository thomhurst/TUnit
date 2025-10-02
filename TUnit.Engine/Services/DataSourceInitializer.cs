using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.PropertyInjection;

namespace TUnit.Engine.Services;

/// <summary>
/// Centralized service responsible for initializing data source instances.
/// Ensures all data sources are properly initialized before use, regardless of where they're used
/// (properties, constructor arguments, or method arguments).
/// </summary>
internal sealed class DataSourceInitializer
{
    private readonly Dictionary<object, Task> _initializationTasks = new();
    private readonly object _lock = new();
    private PropertyInjectionService? _propertyInjectionService;
    
    public void Initialize(PropertyInjectionService propertyInjectionService)
    {
        _propertyInjectionService = propertyInjectionService;
    }

    /// <summary>
    /// Ensures a data source instance is fully initialized before use.
    /// This includes property injection and calling IAsyncInitializer if implemented.
    /// </summary>
    public async Task<T> EnsureInitializedAsync<T>(
        T dataSource,
        Dictionary<string, object?>? objectBag = null,
        MethodMetadata? methodMetadata = null,
        TestContextEvents? events = null) where T : notnull
    {
        if (dataSource == null)
        {
            throw new ArgumentNullException(nameof(dataSource));
        }

        // Check if already initialized or being initialized
        Task? existingTask;
        lock (_lock)
        {
            if (_initializationTasks.TryGetValue(dataSource, out existingTask))
            {
                // Already initialized or being initialized
            }
            else
            {
                // Start initialization
                existingTask = InitializeDataSourceAsync(dataSource, objectBag, methodMetadata, events);
                _initializationTasks[dataSource] = existingTask;
            }
        }

        await existingTask;
        return dataSource;
    }

    /// <summary>
    /// Initializes a data source instance with the complete lifecycle.
    /// </summary>
    private async Task InitializeDataSourceAsync(
        object dataSource,
        Dictionary<string, object?>? objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents? events)
    {
        try
        {
            // Ensure we have required context
            objectBag ??= new Dictionary<string, object?>();
            events ??= new TestContextEvents();

            // Initialize the data source directly here
            // Step 1: Property injection - use PropertyInjectionService if available
            if (_propertyInjectionService != null && PropertyInjectionCache.HasInjectableProperties(dataSource.GetType()))
            {
                await _propertyInjectionService.InjectPropertiesIntoObjectAsync(
                    dataSource, objectBag, methodMetadata, events);
            }
            
            // Step 2: IAsyncInitializer
            if (dataSource is IAsyncInitializer asyncInitializer)
            {
                await asyncInitializer.InitializeAsync();
            }

            // Note: Tracking is now handled by ObjectRegistrationService during registration phase
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to initialize data source of type '{dataSource.GetType().Name}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Clears the initialization cache. Should be called at the end of test sessions.
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            _initializationTasks.Clear();
        }
    }
}