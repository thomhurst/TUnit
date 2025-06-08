using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Data;
using TUnit.Core.Enums;
using TUnit.Core.Helpers;

namespace TUnit.Core;

internal class ClassDataSources
{
    private ClassDataSources()
    {
    }

    public static readonly GetOnlyDictionary<string, ClassDataSources> SourcesPerSession = new();

    public static ClassDataSources Get(string sessionId) => SourcesPerSession.GetOrAdd(sessionId, _ => new());

    public (T, SharedType, string) GetItemForIndex<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(int index, Type testClassType, SharedType[] sharedTypes, string[] keys, DataGeneratorMetadata dataGeneratorMetadata) where T : new()
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

    public T Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(SharedType sharedType, Type testClassType, string key, DataGeneratorMetadata dataGeneratorMetadata)
    {
        if (sharedType == SharedType.None)
        {
            return Create<T>(dataGeneratorMetadata);
        }

        if (sharedType == SharedType.PerTestSession)
        {
            return (T)TestDataContainer.GetGlobalInstance(typeof(T), () => Create<T>(dataGeneratorMetadata));
        }

        if (sharedType == SharedType.PerClass)
        {
            return (T)TestDataContainer.GetInstanceForClass(testClassType, typeof(T), () => Create<T>(dataGeneratorMetadata));
        }

        if (sharedType == SharedType.Keyed)
        {
            return (T)TestDataContainer.GetInstanceForKey(key, typeof(T), () => Create<T>(dataGeneratorMetadata));
        }

        if (sharedType == SharedType.PerAssembly)
        {
            return (T)TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, typeof(T), () => Create<T>(dataGeneratorMetadata));
        }

        throw new ArgumentOutOfRangeException();
    }

    public object Get(SharedType sharedType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, Type testClassType, string? key, DataGeneratorMetadata dataGeneratorMetadata)
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
            return TestDataContainer.GetInstanceForClass(testClassType, type, () => Create(type, dataGeneratorMetadata));
        }

        if (sharedType == SharedType.Keyed)
        {
            return TestDataContainer.GetInstanceForKey(key!, type, () => Create(type, dataGeneratorMetadata));
        }

        if (sharedType == SharedType.PerAssembly)
        {
            return TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, type, () => Create(type, dataGeneratorMetadata));
        }

        throw new ArgumentOutOfRangeException();
    }

    [return: NotNull]
    private static T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return ((T)Create(typeof(T), dataGeneratorMetadata))!;
    }

    private static object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, DataGeneratorMetadata dataGeneratorMetadata)
    {
        try
        {
            var instance = Activator.CreateInstance(type)!;

            if (!Sources.Properties.TryGetValue(instance.GetType(), out var properties))
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            }

            InitializeDataSourceProperties(dataGeneratorMetadata, instance, properties);

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

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    private static void InitializeDataSourceProperties(DataGeneratorMetadata dataGeneratorMetadata, object instance, PropertyInfo[] properties)
    {
        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.GetCustomAttributes().OfType<IDataSourceGeneratorAttribute>().FirstOrDefault() is not { } dataSourceGeneratorAttribute)
            {
                continue;
            }

            if (propertyInfo.GetValue(instance) is not {} result)
            {
                var resultDelegateArray = dataSourceGeneratorAttribute.GenerateDataSourcesInternal(dataGeneratorMetadata with
                {
                    Type = DataGeneratorType.Property, MembersToGenerate = [ReflectionToSourceModelHelpers.GenerateProperty(propertyInfo)]
                });

                result = resultDelegateArray.FirstOrDefault()?.Invoke()?.FirstOrDefault();

                propertyInfo.SetValue(instance, result);
            }

            if (result is null || !dataSourceGeneratorAttribute.GetType().IsAssignableTo(typeof(IDataSourceGeneratorAttribute)))
            {
                return;
            }

            if (!Sources.Properties.TryGetValue(result.GetType(), out var nestedProperties))
            {
                nestedProperties = result.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            }

            InitializeDataSourceProperties(dataGeneratorMetadata, result, nestedProperties);
        }
    }
}
