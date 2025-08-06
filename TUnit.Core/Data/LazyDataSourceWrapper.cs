using System.Runtime.ExceptionServices;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Data;

/// <summary>
/// A wrapper that provides lazy initialization for data source objects that implement IRequiresLazyInitialization.
/// This allows deferring expensive initialization operations (like spinning up test servers) until they're actually needed.
/// </summary>
internal class LazyDataSourceWrapper : IAsyncInitializer
{
    private readonly Type _type;
    private readonly Func<object> _factory;
    private readonly DataGeneratorMetadata? _metadata;
    private readonly Lazy<object> _lazyInstance;
    private volatile bool _isInitialized;
    private volatile Task? _initializationTask;
    private readonly object _initLock = new object();

    public LazyDataSourceWrapper(Type type, Func<object> factory, DataGeneratorMetadata? metadata)
    {
        _type = type ?? throw new ArgumentNullException(nameof(type));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _metadata = metadata;
        _lazyInstance = new Lazy<object>(_factory, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Gets the actual instance, creating it if necessary but WITHOUT initializing it.
    /// This is used during test discovery to get a placeholder instance.
    /// </summary>
    public object GetInstance()
    {
        return _lazyInstance.Value;
    }

    /// <summary>
    /// Gets the actual instance and ensures it's fully initialized.
    /// This should be called during test execution when the instance is actually needed.
    /// </summary>
    public async Task<object> GetInitializedInstanceAsync()
    {
        var instance = GetInstance();
        
        if (!_isInitialized)
        {
            await EnsureInitializedAsync(instance);
        }
        
        return instance;
    }

    /// <summary>
    /// Implements IAsyncInitializer to integrate with existing TUnit initialization infrastructure.
    /// This method triggers the actual initialization of the wrapped instance.
    /// </summary>
    public async Task InitializeAsync()
    {
        var instance = GetInstance();
        await EnsureInitializedAsync(instance);
    }

    private async Task EnsureInitializedAsync(object instance)
    {
        if (_isInitialized)
        {
            return;
        }

        Task? initTask;
        lock (_initLock)
        {
            if (_isInitialized)
            {
                return;
            }

            if (_initializationTask == null)
            {
                _initializationTask = InitializeInstanceAsync(instance);
            }
            
            initTask = _initializationTask;
        }

        try
        {
            await initTask;
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            // Reset the task so retry is possible
            lock (_initLock)
            {
                _initializationTask = null;
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw; // This will never be reached, but satisfies the compiler
        }
    }

    private async Task InitializeInstanceAsync(object instance)
    {
        // Initialize the actual instance if it implements IAsyncInitializer
        if (instance is IAsyncInitializer asyncInitializer)
        {
            await asyncInitializer.InitializeAsync();
        }

        // Also run any property injection if metadata is available
        if (_metadata != null && _metadata.TestInformation != null)
        {
            var objectBag = _metadata.TestBuilderContext?.Current?.ObjectBag ?? new Dictionary<string, object?>();
            var events = _metadata.TestBuilderContext?.Current?.Events;
            
            await PropertyInjectionService.InjectPropertiesIntoObjectAsync(
                instance,
                objectBag,
                _metadata.TestInformation,
                events);
        }
    }

    /// <summary>
    /// Checks if the instance has been created (but not necessarily initialized).
    /// </summary>
    public bool IsInstanceCreated => _lazyInstance.IsValueCreated;

    /// <summary>
    /// Checks if the instance has been both created and initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized && IsInstanceCreated;
}