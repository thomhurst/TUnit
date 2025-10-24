using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Data;

namespace TUnit.Core;

internal class ClassDataSources
{
    private ClassDataSources()
    {
    }

    public static readonly ThreadSafeDictionary<string, ClassDataSources> SourcesPerSession = new();

    public static ClassDataSources Get(string sessionId)
    {
        return SourcesPerSession.GetOrAdd(sessionId, static _ => new ClassDataSources());
    }

    public (T, SharedType, string) GetItemForIndexAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] T>(int index, Type testClassType, SharedType[] sharedTypes, string[] keys, DataGeneratorMetadata dataGeneratorMetadata) where T : new()
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

    public T Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] T>(SharedType sharedType, Type testClassType, string key, DataGeneratorMetadata dataGeneratorMetadata)
    {
        return sharedType switch
        {
            SharedType.None => Create<T>(dataGeneratorMetadata),
            SharedType.PerTestSession => (T) TestDataContainer.GetGlobalInstance(typeof(T), _ => Create(typeof(T), dataGeneratorMetadata))!,
            SharedType.PerClass => (T) TestDataContainer.GetInstanceForClass(testClassType, typeof(T), _ => Create(typeof(T), dataGeneratorMetadata))!,
            SharedType.Keyed => (T) TestDataContainer.GetInstanceForKey(key, typeof(T), _ => Create(typeof(T), dataGeneratorMetadata))!,
            SharedType.PerAssembly => (T) TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, typeof(T), _ => Create(typeof(T), dataGeneratorMetadata))!,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public object? Get(SharedType sharedType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type, Type testClassType, string? key, DataGeneratorMetadata dataGeneratorMetadata)
    {
        return sharedType switch
        {
            SharedType.None => Create(type, dataGeneratorMetadata),
            SharedType.PerTestSession => TestDataContainer.GetGlobalInstance(type, _ => Create(type, dataGeneratorMetadata)),
            SharedType.PerClass => TestDataContainer.GetInstanceForClass(testClassType, type, _ => Create(type, dataGeneratorMetadata)),
            SharedType.Keyed => TestDataContainer.GetInstanceForKey(key!, type, _ => Create(type, dataGeneratorMetadata)),
            SharedType.PerAssembly => TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, type, _ => Create(type, dataGeneratorMetadata)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [return: NotNull]
    private static T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] T>(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return ((T) Create(typeof(T), dataGeneratorMetadata))!;
    }

    private static object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type, DataGeneratorMetadata dataGeneratorMetadata)
    {
        return CreateWithNestedDependencies(type, dataGeneratorMetadata, recursionDepth: 0);
    }

    private const int MaxRecursionDepth = 10;

    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' requirements",
        Justification = "PropertyType from PropertyInjectionMetadata has the required DynamicallyAccessedMembers annotations")]
    private static object CreateWithNestedDependencies([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type, DataGeneratorMetadata dataGeneratorMetadata, int recursionDepth)
    {
        if (recursionDepth >= MaxRecursionDepth)
        {
            throw new InvalidOperationException($"Maximum recursion depth ({MaxRecursionDepth}) exceeded when creating nested ClassDataSource dependencies. This may indicate a circular dependency.");
        }

        object instance;
        try
        {
            instance = Activator.CreateInstance(type)!;
        }
        catch (TargetInvocationException targetInvocationException)
        {
            if (targetInvocationException.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(targetInvocationException.InnerException).Throw();
            }

            throw;
        }

        // Populate nested ClassDataSource properties recursively
        var propertySource = PropertySourceRegistry.GetSource(type);
        if (propertySource?.ShouldInitialize == true)
        {
            var propertyMetadata = propertySource.GetPropertyMetadata();
            foreach (var metadata in propertyMetadata)
            {
                // Recursively create the property value using CreateWithNestedDependencies
                // This will handle nested ClassDataSource properties
                var propertyValue = CreateWithNestedDependencies(metadata.PropertyType, dataGeneratorMetadata, recursionDepth + 1);
                metadata.SetProperty(instance, propertyValue);
            }
        }

        return instance;
    }
}
