using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Data;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;

namespace TUnit.Core;

internal class ClassDataSources
{
    private ClassDataSources()
    {
    }

    public static readonly GetOnlyDictionary<string, ClassDataSources> SourcesPerSession = new();

    public static ClassDataSources Get(string sessionId) => SourcesPerSession.GetOrAdd(sessionId, _ => new());

    public (T, SharedType, string) GetItemForIndexAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] T>(int index, Type? testClassType, SharedType[] sharedTypes, string[] keys, DataGeneratorMetadata dataGeneratorMetadata) where T : new()
    {
        var shared = sharedTypes.ElementAtOrDefault(index);

        var key = shared == SharedType.Keyed ? GetKey(index, sharedTypes, keys) : string.Empty;

        return
        (
            Get<T>(shared, testClassType, key, dataGeneratorMetadata),
            shared,
            key
        );
    }

    private string GetKey(int index, SharedType[] sharedTypes, string[] keys)
    {
        var keyedIndex = sharedTypes.Take(index + 1).Count(x => x == SharedType.Keyed) - 1;

        return keys.ElementAtOrDefault(keyedIndex) ?? throw new ArgumentException($"Key at index {keyedIndex} not found");
    }

    public T Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] T>(SharedType sharedType, Type? testClassType, string key, DataGeneratorMetadata dataGeneratorMetadata)
    {
#pragma warning disable CS8603 // Possible null reference return.
        if (sharedType == SharedType.None)
        {
            return Create<T>(dataGeneratorMetadata);
        }

        if (sharedType == SharedType.PerTestSession)
        {
            var result = TestDataContainer.GetGlobalInstance(typeof(T), () => Create(typeof(T), dataGeneratorMetadata));
            return HandleResult<T>(result);
        }

        if (sharedType == SharedType.PerClass)
        {
            if (testClassType == null)
            {
                throw new InvalidOperationException($"Cannot use SharedType.PerClass without a test class type. This may occur during static property initialization.");
            }
            var result = TestDataContainer.GetInstanceForClass(testClassType, typeof(T), () => Create(typeof(T), dataGeneratorMetadata));
            return HandleResult<T>(result);
        }

        if (sharedType == SharedType.Keyed)
        {
            var result = TestDataContainer.GetInstanceForKey(key, typeof(T), () => Create(typeof(T), dataGeneratorMetadata));
            return HandleResult<T>(result);
        }

        if (sharedType == SharedType.PerAssembly)
        {
            if (testClassType == null)
            {
                throw new InvalidOperationException($"Cannot use SharedType.PerAssembly without a test class type. This may occur during static property initialization.");
            }
            var result = TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, typeof(T), () => Create(typeof(T), dataGeneratorMetadata));
            return HandleResult<T>(result);
        }
#pragma warning restore CS8603 // Possible null reference return.

        throw new ArgumentOutOfRangeException();
    }

    public object Get(SharedType sharedType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type type, Type? testClassType, string? key, DataGeneratorMetadata dataGeneratorMetadata)
    {
        if (sharedType == SharedType.None)
        {
            return Create(type, dataGeneratorMetadata);
        }

        if (sharedType == SharedType.PerTestSession)
        {
            var result = TestDataContainer.GetGlobalInstance(type, () => Create(type, dataGeneratorMetadata));
            return HandleResult(result, type);
        }

        if (sharedType == SharedType.PerClass)
        {
            if (testClassType == null)
            {
                throw new InvalidOperationException($"Cannot use SharedType.PerClass without a test class type. This may occur during static property initialization.");
            }
            var result = TestDataContainer.GetInstanceForClass(testClassType, type, () => Create(type, dataGeneratorMetadata));
            return HandleResult(result, type);
        }

        if (sharedType == SharedType.Keyed)
        {
            var result = TestDataContainer.GetInstanceForKey(key!, type, () => Create(type, dataGeneratorMetadata));
            return HandleResult(result, type);
        }

        if (sharedType == SharedType.PerAssembly)
        {
            if (testClassType == null)
            {
                throw new InvalidOperationException($"Cannot use SharedType.PerAssembly without a test class type. This may occur during static property initialization.");
            }
            var result = TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, type, () => Create(type, dataGeneratorMetadata));
            return HandleResult(result, type);
        }

        throw new ArgumentOutOfRangeException();
    }

    /// <summary>
    /// Handles the result from TestDataContainer, which might be a LazyDataSourceWrapper or the actual instance.
    /// For lazy wrappers, returns the instance without initializing it (for discovery).
    /// For regular instances, returns them as-is.
    /// </summary>
    private static T HandleResult<T>(object result)
    {
        if (result is LazyDataSourceWrapper wrapper)
        {
            // During test discovery, we want the instance but not initialized
            return (T)wrapper.GetInstance();
        }
        
        return (T)result;
    }

    /// <summary>
    /// Non-generic version of HandleResult.
    /// </summary>
    private static object HandleResult(object result, Type expectedType)
    {
        if (result is LazyDataSourceWrapper wrapper)
        {
            // During test discovery, we want the instance but not initialized
            return wrapper.GetInstance();
        }
        
        return result;
    }

    [return: NotNull]
    private static T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] T>(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return ((T) Create(typeof(T), dataGeneratorMetadata))!;
    }

    private static object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type type, DataGeneratorMetadata dataGeneratorMetadata)
    {
        try
        {
            var instance = Activator.CreateInstance(type)!;

            // Track the created object
            var trackerEvents2 = dataGeneratorMetadata.TestBuilderContext?.Current.Events;

            if (trackerEvents2 != null)
            {
                ObjectTracker.TrackObject(trackerEvents2, instance);
            }

            // CRITICAL CHANGE: Only initialize synchronously if the type does NOT require lazy initialization
            // This prevents eager initialization of expensive resources during test discovery
            if (typeof(IRequiresLazyInitialization).IsAssignableFrom(type))
            {
                // For types that require lazy initialization, we skip the immediate initialization
                // The LazyDataSourceWrapper will handle initialization when actually needed
                return instance;
            }

            // For types that don't require lazy initialization, initialize immediately as before
            // to maintain backward compatibility
            if (dataGeneratorMetadata.TestInformation != null)
            {
                var initTask = Helpers.DataSourceHelpers.InitializeDataSourcePropertiesAsync(
                    instance,
                    dataGeneratorMetadata.TestInformation,
                    dataGeneratorMetadata.TestSessionId);

                // We need to block here since this method isn't async
                initTask.GetAwaiter().GetResult();

                // Also try PropertyInjectionService for properties that have data source attributes
                // This handles cases where the type doesn't have a generated initializer
                var objectBag = dataGeneratorMetadata.TestBuilderContext?.Current?.ObjectBag ?? new Dictionary<string, object?>();
                var events = dataGeneratorMetadata.TestBuilderContext?.Current?.Events;
                var injectionTask = PropertyInjectionService.InjectPropertiesIntoObjectAsync(
                    instance,
                    objectBag,
                    dataGeneratorMetadata.TestInformation,
                    events);
                injectionTask.GetAwaiter().GetResult();
            }

            return instance;
        }
        catch (TargetInvocationException targetInvocationException)
        {
            if (targetInvocationException.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(targetInvocationException.InnerException).Throw();
            }

            throw;
        }
    }
}
