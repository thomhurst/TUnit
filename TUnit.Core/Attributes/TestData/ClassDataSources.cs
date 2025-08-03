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
            return Activator.CreateInstance(type)!;
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
