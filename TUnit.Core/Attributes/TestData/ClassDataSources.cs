using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Data;
using TUnit.Core.Enums;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;

#pragma warning disable CS0618 // Type or member is obsolete

namespace TUnit.Core;

internal class ClassDataSources
{
    private ClassDataSources()
    {
    }

    public GetOnlyDictionary<Type, Task> GlobalInitializers = new();
    public readonly GetOnlyDictionary<Type, GetOnlyDictionary<Type, Task>> TestClassTypeInitializers = new();
    public readonly GetOnlyDictionary<Type, GetOnlyDictionary<Assembly, Task>> AssemblyInitializers = new();
    public readonly GetOnlyDictionary<Type, GetOnlyDictionary<string, Task>> KeyedInitializers = new();

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

    public object Get(SharedType sharedType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, Type testClassType, string key, DataGeneratorMetadata dataGeneratorMetadata)
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
            return TestDataContainer.GetInstanceForKey(key, type, () => Create(type, dataGeneratorMetadata));
        }

        if (sharedType == SharedType.PerAssembly)
        {
            return TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, type, () => Create(type, dataGeneratorMetadata));
        }

        throw new ArgumentOutOfRangeException();
    }

    public Task InitializeObject(object? item)
    {
        if (item is IAsyncInitializer asyncInitializer)
        {
            return asyncInitializer.InitializeAsync();
        }

        return Task.CompletedTask;
    }

    public async ValueTask OnTestRegistered<T>(TestContext testContext, bool isStatic, SharedType shared, string key, T? item)
    {
        switch (shared)
        {
            case SharedType.None:
                break;
            case SharedType.PerClass:
                TestDataContainer.IncrementTestClassUsage(testContext.TestDetails.TestClass.Type, typeof(T));
                break;
            case SharedType.PerAssembly:
                TestDataContainer.IncrementAssemblyUsage(testContext.TestDetails.TestClass.Type.Assembly, typeof(T));
                break;
            case SharedType.PerTestSession:
                TestDataContainer.IncrementGlobalUsage(typeof(T));
                break;
            case SharedType.Keyed:
                TestDataContainer.IncrementKeyUsage(key, typeof(T));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (isStatic)
        {
            await Initialize(testContext, shared, key, item);
        }

        if (item is ITestRegisteredEventReceiver testRegisteredEventReceiver)
        {
            await testRegisteredEventReceiver.OnTestRegistered(new TestRegisteredContext(testContext.InternalDiscoveredTest));
        }
    }

    public async ValueTask OnInitialize<T>(TestContext testContext, bool isStatic, SharedType shared, string key, T? item)
    {
        if (isStatic)
        {
            // Done already before test start
            return;
        }

        await Initialize(testContext, shared, key, item);
    }

    public Task Initialize<T>(TestContext testContext, SharedType shared, string key, T? item)
    {
        if (shared == SharedType.PerTestSession)
        {
            return GlobalInitializers.GetOrAdd(typeof(T), _ => InitializeObject(item));
        }

        if (shared == SharedType.None)
        {
            return InitializeObject(item);
        }

        if (shared == SharedType.PerClass)
        {
            var innerDictionary = TestClassTypeInitializers.GetOrAdd(typeof(T),
                _ => new GetOnlyDictionary<Type, Task>());

            return innerDictionary.GetOrAdd(testContext.TestDetails.TestClass.Type,
                _ => InitializeObject(item));
        }

        if (shared == SharedType.PerAssembly)
        {
            var innerDictionary = AssemblyInitializers.GetOrAdd(typeof(T),
                _ => new GetOnlyDictionary<Assembly, Task>());

            return innerDictionary.GetOrAdd(testContext.TestDetails.TestClass.Type.Assembly,
                _ => InitializeObject(item));
        }

        if (shared == SharedType.Keyed)
        {
            var innerDictionary = KeyedInitializers.GetOrAdd(typeof(T),
                _ => new GetOnlyDictionary<string, Task>());

            return innerDictionary.GetOrAdd(key, _ => InitializeObject(item));
        }

        throw new ArgumentOutOfRangeException(nameof(shared));
    }

    public async ValueTask OnDispose<T>(TestContext testContext, SharedType shared, string key, T? item)
    {
        if (shared is SharedType.None)
        {
            await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(item);
        }

        if (shared == SharedType.Keyed)
        {
            await TestDataContainer.ConsumeKey(key, typeof(T));
        }

        if (shared == SharedType.PerClass)
        {
            await TestDataContainer.ConsumeTestClassCount(testContext.TestDetails.TestClass.Type, item);
        }

        if (shared == SharedType.PerAssembly)
        {
            await TestDataContainer.ConsumeAssemblyCount(testContext.TestDetails.TestClass.Type.Assembly, item);
        }

        if (shared == SharedType.PerTestSession)
        {
            await TestDataContainer.ConsumeGlobalCount(item);
        }
    }

    public static bool IsStaticProperty(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return dataGeneratorMetadata.MembersToGenerate is [SourceGeneratedPropertyInformation { IsStatic: true }];
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

            if (!Sources.DataGeneratorProperties.TryGetValue(instance.GetType(), out var properties))
            {
                GlobalContext.Current.GlobalLogger.LogDebug("No Source Generated Properties found for {type.FullName}");
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            }
            else
            {
                GlobalContext.Current.GlobalLogger.LogDebug($"Found Source Generated Properties for {type.FullName}");
            }

            GlobalContext.Current.GlobalLogger.LogDebug($"Properties found for {type.FullName}: {properties.Length}");

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

            var resultDelegateArray = dataSourceGeneratorAttribute.GenerateDataSourcesInternal(dataGeneratorMetadata with
            {
                Type = DataGeneratorType.Property,
                MembersToGenerate = [ReflectionToSourceModelHelpers.GenerateProperty(propertyInfo)]
            });

            var result = resultDelegateArray.FirstOrDefault()?.Invoke()?.FirstOrDefault();

            propertyInfo.SetValue(instance, result);

            if (result is not null && dataSourceGeneratorAttribute.GetType().IsAssignableTo(typeof(IDataSourceGeneratorAttribute)))
            {
                var sharedTypeProperty = dataSourceGeneratorAttribute.GetType()
                    .GetProperty(nameof(ClassDataSourceAttribute<object>.Shared));

                var sharedType = sharedTypeProperty?.GetValue(dataSourceGeneratorAttribute) as SharedType? ?? SharedType.None;

                var keyProperty = dataSourceGeneratorAttribute.GetType()
                    .GetProperty(nameof(ClassDataSourceAttribute<object>.Key));
                var key = keyProperty?.GetValue(dataSourceGeneratorAttribute) as string ?? string.Empty;

                TestDataContainer.RegisterNestedDependency(instance, result, sharedType, key);

                // Register the nested dependency with the test lifecycle - this is the key fix!
                // We need to register it as if it were a main ClassDataSource so it gets proper usage tracking
                RegisterNestedDependencyWithLifecycle(result, sharedType, key, dataGeneratorMetadata);
            }

            if (result is not null)
            {
                if (!Sources.DataGeneratorProperties.TryGetValue(result.GetType(), out var nestedProperties))
                {
                    nestedProperties = result.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                }

                InitializeDataSourceProperties(dataGeneratorMetadata, result, nestedProperties);
            }
        }
    }

    public async Task OnTestStart<T>(BeforeTestContext context, T item)
    {
        if (item is ITestStartEventReceiver testStartEventReceiver)
        {
            await testStartEventReceiver.OnTestStart(context);
        }
    }

    public async Task OnTestEnd<T>(AfterTestContext context, T item)
    {
        if (item is ITestEndEventReceiver testEndEventReceiver)
        {
            await testEndEventReceiver.OnTestEnd(context);
        }
    }

    /// <summary>
    /// Registers a nested dependency with proper usage tracking for its shared type.
    /// </summary>
    /// <param name="nestedObject">The nested dependency object.</param>
    /// <param name="sharedType">The shared type of the nested dependency.</param>
    /// <param name="key">The key for keyed sharing.</param>
    /// <param name="dataGeneratorMetadata">The data generator metadata.</param>
    private static void RegisterNestedDependencyWithLifecycle(object nestedObject, SharedType sharedType, string key, DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Increment usage count for the nested dependency based on its shared type
        // This ensures it gets the same usage tracking as if it were a main ClassDataSource
        switch (sharedType)
        {
            case SharedType.None:
                // No usage tracking needed for non-shared objects
                break;
            case SharedType.PerClass:
                TestDataContainer.IncrementTestClassUsage(dataGeneratorMetadata.TestClassType, nestedObject.GetType());
                break;
            case SharedType.PerAssembly:
                TestDataContainer.IncrementAssemblyUsage(dataGeneratorMetadata.TestClassType.Assembly, nestedObject.GetType());
                break;
            case SharedType.PerTestSession:
                TestDataContainer.IncrementGlobalUsage(nestedObject.GetType());
                break;
            case SharedType.Keyed:
                TestDataContainer.IncrementKeyUsage(key, nestedObject.GetType());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sharedType));
        }
    }
}
