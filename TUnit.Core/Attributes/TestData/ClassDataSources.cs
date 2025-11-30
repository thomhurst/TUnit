using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Data;
using TUnit.Core.PropertyInjection;

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
        return Create(type, dataGeneratorMetadata, recursionDepth: 0);
    }

    private const int MaxRecursionDepth = 10;

    private static object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type, DataGeneratorMetadata dataGeneratorMetadata, int recursionDepth)
    {
        if (recursionDepth >= MaxRecursionDepth)
        {
            throw new InvalidOperationException($"Maximum recursion depth ({MaxRecursionDepth}) exceeded when creating nested ClassDataSource dependencies. This may indicate a circular dependency.");
        }

        try
        {
            var instance = Activator.CreateInstance(type)!;

            // Inject properties into the created instance
            InjectPropertiesSync(instance, type, dataGeneratorMetadata, recursionDepth);

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
    /// Injects properties into an instance synchronously.
    /// Used when creating instances via ClassDataSource for nested data source dependencies.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Type is already annotated with DynamicallyAccessedMembers")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling", Justification = "Fallback to reflection mode when source-gen not available")]
    private static void InjectPropertiesSync(
        object instance,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type,
        DataGeneratorMetadata dataGeneratorMetadata,
        int recursionDepth)
    {
        // Get the injection plan for this type
        var plan = PropertyInjectionPlanBuilder.Build(type);
        if (!plan.HasProperties)
        {
            return;
        }

        // Handle source-generated properties
        foreach (var metadata in plan.SourceGeneratedProperties)
        {
            var dataSource = metadata.CreateDataSource();
            var propertyMetadata = CreatePropertyMetadata(type, metadata.PropertyName, metadata.PropertyType);

            var propertyDataGeneratorMetadata = DataGeneratorMetadataCreator.CreateForPropertyInjection(
                propertyMetadata,
                dataGeneratorMetadata.TestInformation,
                dataSource,
                testContext: null,
                testClassInstance: instance,
                events: new TestContextEvents(),
                objectBag: new ConcurrentDictionary<string, object?>());

            var value = ResolveDataSourceValueSync(dataSource, propertyDataGeneratorMetadata, recursionDepth + 1);
            if (value != null)
            {
                metadata.SetProperty(instance, value);
            }
        }

        // Handle reflection-mode properties
        foreach (var (property, dataSource) in plan.ReflectionProperties)
        {
            var propertyMetadata = CreatePropertyMetadataFromPropertyInfo(property);

            var propertyDataGeneratorMetadata = DataGeneratorMetadataCreator.CreateForPropertyInjection(
                propertyMetadata,
                dataGeneratorMetadata.TestInformation,
                dataSource,
                testContext: null,
                testClassInstance: instance,
                events: new TestContextEvents(),
                objectBag: new ConcurrentDictionary<string, object?>());

            var value = ResolveDataSourceValueSync(dataSource, propertyDataGeneratorMetadata, recursionDepth + 1);
            if (value != null)
            {
                SetPropertyValue(property, instance, value);
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method", Justification = "Type is already annotated in caller")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method return value does not satisfy 'DynamicallyAccessedMembersAttribute'", Justification = "Type is already annotated in caller")]
    private static PropertyMetadata CreatePropertyMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type containingType,
        string propertyName,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type propertyType)
    {
        return new PropertyMetadata
        {
            Name = propertyName,
            Type = propertyType,
            IsStatic = false,
            ClassMetadata = GetClassMetadataForType(containingType),
            ContainingTypeMetadata = GetClassMetadataForType(containingType),
            ReflectionInfo = containingType.GetProperty(propertyName)!,
            Getter = parent => containingType.GetProperty(propertyName)?.GetValue(parent)
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "PropertyInfo already obtained")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:'value' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method", Justification = "PropertyInfo already obtained with type annotations")]
    private static PropertyMetadata CreatePropertyMetadataFromPropertyInfo(PropertyInfo property)
    {
        var containingType = property.DeclaringType!;
        return new PropertyMetadata
        {
            Name = property.Name,
            Type = property.PropertyType,
            IsStatic = property.GetMethod?.IsStatic ?? false,
            ClassMetadata = GetClassMetadataForType(containingType),
            ContainingTypeMetadata = GetClassMetadataForType(containingType),
            ReflectionInfo = property,
            Getter = parent => property.GetValue(parent)
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Type is already annotated")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method return value does not satisfy 'DynamicallyAccessedMembersAttribute'", Justification = "Type is already annotated")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method", Justification = "Type is already annotated")]
    [UnconditionalSuppressMessage("Trimming", "IL2067:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method", Justification = "Type is already annotated")]
    private static ClassMetadata GetClassMetadataForType(Type type)
    {
        return ClassMetadata.GetOrAdd(type.FullName ?? type.Name, () =>
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var constructor = constructors.FirstOrDefault();

            var constructorParameters = constructor?.GetParameters().Select((p, i) => new ParameterMetadata(p.ParameterType)
            {
                Name = p.Name ?? $"param{i}",
                TypeInfo = new ConcreteType(p.ParameterType),
                ReflectionInfo = p
            }).ToArray() ?? [];

            return new ClassMetadata
            {
                Type = type,
                TypeInfo = new ConcreteType(type),
                Name = type.Name,
                Namespace = type.Namespace ?? string.Empty,
                Assembly = AssemblyMetadata.GetOrAdd(type.Assembly.GetName().Name ?? type.Assembly.GetName().FullName ?? "Unknown", () => new AssemblyMetadata
                {
                    Name = type.Assembly.GetName().Name ?? type.Assembly.GetName().FullName ?? "Unknown"
                }),
                Properties = [],
                Parameters = constructorParameters,
                Parent = type.DeclaringType != null ? GetClassMetadataForType(type.DeclaringType) : null
            };
        });
    }

    /// <summary>
    /// Resolves a data source value synchronously by running the async enumerable.
    /// </summary>
    private static object? ResolveDataSourceValueSync(IDataSourceAttribute dataSource, DataGeneratorMetadata metadata, int recursionDepth)
    {
        var dataRows = dataSource.GetDataRowsAsync(metadata);

        // Get the first value from the async enumerable synchronously
        var enumerator = dataRows.GetAsyncEnumerator();
        try
        {
            if (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
            {
                var factory = enumerator.Current;
                var args = factory().GetAwaiter().GetResult();
                if (args is { Length: > 0 })
                {
                    var value = args[0];

                    // Initialize the value if it implements IAsyncInitializer
                    ObjectInitializer.InitializeAsync(value).AsTask().GetAwaiter().GetResult();

                    return value;
                }
            }
        }
        finally
        {
            enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        return null;
    }

    /// <summary>
    /// Sets a property value, handling init-only properties via backing field if necessary.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "PropertyInfo already obtained")]
    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method", Justification = "PropertyInfo already obtained with type annotations")]
    private static void SetPropertyValue(PropertyInfo property, object instance, object? value)
    {
        if (property.CanWrite && property.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        // Try to set via backing field for init-only properties
        var backingFieldName = $"<{property.Name}>k__BackingField";
        var backingField = property.DeclaringType?.GetField(
            backingFieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (backingField != null)
        {
            backingField.SetValue(instance, value);
        }
    }
}
