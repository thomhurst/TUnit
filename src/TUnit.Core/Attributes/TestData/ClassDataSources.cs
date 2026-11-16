using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Data;
using TUnit.Core.Interfaces;

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
            SharedType.None => Create<T>(),
            SharedType.PerTestSession => (T) TestDataContainer.GetGlobalInstance(typeof(T), static _ => Create<T>()!)!,
            SharedType.PerClass => (T) TestDataContainer.GetInstanceForClass(testClassType, typeof(T), static _ => Create<T>()!)!,
            SharedType.Keyed => (T) TestDataContainer.GetInstanceForKey(key, typeof(T), _ => CreateWithKey<T>(key))!,
            SharedType.PerAssembly => (T) TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, typeof(T), static _ => Create<T>()!)!,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public object? Get(SharedType sharedType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type, Type testClassType, string? key, DataGeneratorMetadata dataGeneratorMetadata)
    {
        return sharedType switch
        {
            SharedType.None => Create(type),
            SharedType.PerTestSession => TestDataContainer.GetGlobalInstance(type, _ => Create(type)),
            SharedType.PerClass => TestDataContainer.GetInstanceForClass(testClassType, type, _ => Create(type)),
            SharedType.Keyed => TestDataContainer.GetInstanceForKey(key!, type, _ => CreateWithKey(type, key!)),
            SharedType.PerAssembly => TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, type, _ => Create(type)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static object CreateWithKey([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type, string key)
    {
        return SetKey(Create(type), key);
    }

    private static object CreateWithKey<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string key)
    {
        return SetKey(Create<T>(), key);
    }

    private static object SetKey(object instance, string key)
    {
        if (instance is IKeyedDataSource keyed)
        {
            keyed.Key = key;
        }

        return instance;
    }

    [return: NotNull]
    private static T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>()
    {
        try
        {
            // The compiler resolves T's constructor, so nothing depends on run-time constructor metadata.
            return Activator.CreateInstance<T>()!;
        }
        catch (TargetInvocationException targetInvocationException)
        {
            ThrowInnerException(targetInvocationException);
            throw;
        }
    }

    private static object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
    {
        try
        {
            // Just create the instance - initialization happens in the Engine
            return Activator.CreateInstance(type)!;
        }
        catch (TargetInvocationException targetInvocationException)
        {
            ThrowInnerException(targetInvocationException);
            throw;
        }
    }

    // A constructor's own exception, not the reflection wrapper, is what the test should report.
    private static void ThrowInnerException(TargetInvocationException targetInvocationException)
    {
        if (targetInvocationException.InnerException is { } innerException)
        {
            ExceptionDispatchInfo.Capture(innerException).Throw();
        }
    }
}
