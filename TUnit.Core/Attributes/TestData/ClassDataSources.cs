using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Data;
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
            return (T) TestDataContainer.GetGlobalInstance(typeof(T), () => Create(typeof(T), dataGeneratorMetadata));
        }

        if (sharedType == SharedType.PerClass)
        {
            if (testClassType == null)
            {
                throw new InvalidOperationException($"Cannot use SharedType.PerClass without a test class type. This may occur during static property initialization.");
            }
            return (T) TestDataContainer.GetInstanceForClass(testClassType, typeof(T), () => Create(typeof(T), dataGeneratorMetadata));
        }

        if (sharedType == SharedType.Keyed)
        {
            return (T) TestDataContainer.GetInstanceForKey(key, typeof(T), () => Create(typeof(T), dataGeneratorMetadata));
        }

        if (sharedType == SharedType.PerAssembly)
        {
            if (testClassType == null)
            {
                throw new InvalidOperationException($"Cannot use SharedType.PerAssembly without a test class type. This may occur during static property initialization.");
            }
            return (T) TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, typeof(T), () => Create(typeof(T), dataGeneratorMetadata));
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
            return TestDataContainer.GetGlobalInstance(type, () => Create(type, dataGeneratorMetadata));
        }

        if (sharedType == SharedType.PerClass)
        {
            if (testClassType == null)
            {
                throw new InvalidOperationException($"Cannot use SharedType.PerClass without a test class type. This may occur during static property initialization.");
            }
            return TestDataContainer.GetInstanceForClass(testClassType, type, () => Create(type, dataGeneratorMetadata));
        }

        if (sharedType == SharedType.Keyed)
        {
            return TestDataContainer.GetInstanceForKey(key!, type, () => Create(type, dataGeneratorMetadata));
        }

        if (sharedType == SharedType.PerAssembly)
        {
            if (testClassType == null)
            {
                throw new InvalidOperationException($"Cannot use SharedType.PerAssembly without a test class type. This may occur during static property initialization.");
            }
            return TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, type, () => Create(type, dataGeneratorMetadata));
        }

        throw new ArgumentOutOfRangeException();
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

            // Track the created object with scope information
            var trackerEvents2 = dataGeneratorMetadata.TestBuilderContext?.Current.Events;

            if (trackerEvents2 != null)
            {
                // Determine scope and scope key based on how the object was created
                // This is a bit of a hack, but it works without changing the entire creation chain
                var scope = DetermineObjectScope(instance, dataGeneratorMetadata.TestInformation?.TestClassType);
                var scopeKey = GetScopeKey(scope, dataGeneratorMetadata.TestInformation?.TestClassType);
                ObjectTracker.TrackObject(trackerEvents2, instance, scope, scopeKey);
            }

            // Initialize any data source properties on the created instance
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

    /// <summary>
    /// Determines the scope of an object based on where it's stored in the containers
    /// </summary>
    private static SharedType DetermineObjectScope(object instance, Type? testClassType)
    {
        if (testClassType == null)
        {
            return SharedType.None;
        }

        // Check if the object exists in assembly container
        if (TestDataContainer.TryGetInstanceForAssembly(testClassType.Assembly, instance.GetType(), out var assemblyInstance) && 
            ReferenceEquals(instance, assemblyInstance))
        {
            return SharedType.PerAssembly;
        }

        // Check if the object exists in class container
        if (TestDataContainer.TryGetInstanceForClass(testClassType, instance.GetType(), out var classInstance) && 
            ReferenceEquals(instance, classInstance))
        {
            return SharedType.PerClass;
        }

        // Check if the object exists in global container
        if (TestDataContainer.TryGetGlobalInstance(instance.GetType(), out var globalInstance) && 
            ReferenceEquals(instance, globalInstance))
        {
            return SharedType.PerTestSession;
        }

        // Default to test-scoped
        return SharedType.None;
    }

    /// <summary>
    /// Gets the scope key for an object based on its scope
    /// </summary>
    private static object? GetScopeKey(SharedType scope, Type? testClassType)
    {
        return scope switch
        {
            SharedType.PerClass => testClassType,
            SharedType.PerAssembly => testClassType?.Assembly,
            SharedType.PerTestSession => "global",
            _ => null
        };
    }
}
