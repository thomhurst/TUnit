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

    public static ClassDataSources Get(string sessionId)
    {
        var isNew = false;
        var result = SourcesPerSession.GetOrAdd(sessionId, _ =>
        {
            isNew = true;
            return new ClassDataSources();
        });
        
        if (isNew)
        {
            Console.WriteLine($"[ClassDataSources] Created new ClassDataSources for session {sessionId}");
        }
        
        return result;
    }

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
        T instance;
        
        if (sharedType == SharedType.None)
        {
            instance = Create<T>(dataGeneratorMetadata);
        }
        else if (sharedType == SharedType.PerTestSession)
        {
            instance = (T) TestDataContainer.GetGlobalInstance(typeof(T), () => Create(typeof(T), dataGeneratorMetadata));
        }
        else if (sharedType == SharedType.PerClass)
        {
            if (testClassType == null)
            {
                throw new InvalidOperationException($"Cannot use SharedType.PerClass without a test class type. This may occur during static property initialization.");
            }
            instance = (T) TestDataContainer.GetInstanceForClass(testClassType, typeof(T), () => Create(typeof(T), dataGeneratorMetadata));
        }
        else if (sharedType == SharedType.Keyed)
        {
            instance = (T) TestDataContainer.GetInstanceForKey(key, typeof(T), () => Create(typeof(T), dataGeneratorMetadata));
        }
        else if (sharedType == SharedType.PerAssembly)
        {
            if (testClassType == null)
            {
                throw new InvalidOperationException($"Cannot use SharedType.PerAssembly without a test class type. This may occur during static property initialization.");
            }
            var isNew = false;
            instance = (T) TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, typeof(T), () =>
            {
                isNew = true;
                return Create(typeof(T), dataGeneratorMetadata);
            });
            
            // Debug logging for shared objects
            if (typeof(T).Name.Contains("SomeClass"))
            {
                Console.WriteLine($"[ClassDataSources] {(isNew ? "Created new" : "Retrieved existing")} {typeof(T).Name} from PerAssembly cache (hash: {instance.GetHashCode()}, testClassType: {testClassType.Name}, assembly: {testClassType.Assembly.GetName().Name})");
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException();
        }

        // Track the instance for disposal - pure reference counting
        // Each test that uses an object increments its reference count
        // Object is only disposed when ALL tests using it have completed (count reaches zero)
        var trackerEvents = dataGeneratorMetadata.TestBuilderContext.Current.Events;
        ObjectTracker.TrackObject(trackerEvents, instance);

        return instance;
#pragma warning restore CS8603 // Possible null reference return.
    }

    public object Get(SharedType sharedType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type type, Type? testClassType, string? key, DataGeneratorMetadata dataGeneratorMetadata)
    {
        object instance;
        
        if (sharedType == SharedType.None)
        {
            instance = Create(type, dataGeneratorMetadata);
        }
        else if (sharedType == SharedType.PerTestSession)
        {
            instance = TestDataContainer.GetGlobalInstance(type, () => Create(type, dataGeneratorMetadata));
        }
        else if (sharedType == SharedType.PerClass)
        {
            if (testClassType == null)
            {
                throw new InvalidOperationException($"Cannot use SharedType.PerClass without a test class type. This may occur during static property initialization.");
            }
            instance = TestDataContainer.GetInstanceForClass(testClassType, type, () => Create(type, dataGeneratorMetadata));
        }
        else if (sharedType == SharedType.Keyed)
        {
            instance = TestDataContainer.GetInstanceForKey(key!, type, () => Create(type, dataGeneratorMetadata));
        }
        else if (sharedType == SharedType.PerAssembly)
        {
            if (testClassType == null)
            {
                throw new InvalidOperationException($"Cannot use SharedType.PerAssembly without a test class type. This may occur during static property initialization.");
            }
            instance = TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, type, () => Create(type, dataGeneratorMetadata));
        }
        else
        {
            throw new ArgumentOutOfRangeException();
        }

        // Track the instance for disposal - pure reference counting
        // Each test that uses an object increments its reference count
        // Object is only disposed when ALL tests using it have completed (count reaches zero)
        var trackerEvents = dataGeneratorMetadata.TestBuilderContext.Current.Events;
        ObjectTracker.TrackObject(trackerEvents, instance);

        return instance;
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

            // Note: Object tracking is now handled at the higher level in Get() methods
            // to ensure both new and reused shared objects are tracked properly

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
