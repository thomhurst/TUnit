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

    public Task InitializeObject(object? item)
    {
        return ObjectInitializer.InitializeAsync(item, CancellationToken.None);
    }

    public async ValueTask OnTestRegistered<T>(TestContext testContext, bool isStatic, SharedType shared, string? key, T? item)
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
                TestDataContainer.IncrementKeyUsage(key!, typeof(T));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (isStatic)
        {
            await Initialize(testContext, shared, key!, item);
        }

        if (item is ITestRegisteredEventReceiver testRegisteredEventReceiver)
        {
            await testRegisteredEventReceiver.OnTestRegistered(new TestRegisteredContext(testContext.InternalDiscoveredTest));
        }
    }

    public async ValueTask OnInitialize<T>(TestContext testContext, bool isStatic, SharedType shared, string? key, T? item)
    {
        if (isStatic)
        {
            // Done already before test start
            return;
        }

        await Initialize(testContext, shared, key, item);
    }

    public Task Initialize<T>(TestContext testContext, SharedType shared, string? key, T? item)
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

            return innerDictionary.GetOrAdd(key!, _ => InitializeObject(item));
        }

        throw new ArgumentOutOfRangeException(nameof(shared));
    }

    public async ValueTask OnDispose<T>(TestContext testContext, SharedType shared, string? key, T? item)
    {
        if (shared is SharedType.None)
        {
            await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(item);
        }

        if (shared == SharedType.Keyed)
        {
            await TestDataContainer.ConsumeKey(key!, typeof(T));
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

            // Get shared type and key from the attribute if available
            var sharedDataSourceAttribute = propertyInfo.GetCustomAttributes(true)
                .OfType<ISharedDataSourceAttribute>()
                .FirstOrDefault();

            var sharedType = sharedDataSourceAttribute?.GetSharedTypes().ElementAtOrDefault(0) ?? SharedType.None;
            var keys = sharedDataSourceAttribute?.GetKeys().ElementAtOrDefault(0);

            RegisterEvents(result, dataGeneratorMetadata, sharedType, keys);
        }
    }

    public static void RegisterEvents(object? item, DataGeneratorMetadata dataGeneratorMetadata, SharedType sharedType, string? key)
    {
        dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestRegistered += async (_, context) =>
            {
                await Get(dataGeneratorMetadata.TestSessionId).OnTestRegistered(
                    context.TestContext,
                    IsStaticProperty(dataGeneratorMetadata),
                    sharedType,
                    key,
                    item);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnInitialize += async (_, context) =>
            {
                await Get(dataGeneratorMetadata.TestSessionId).OnInitialize(
                    context,
                    IsStaticProperty(dataGeneratorMetadata),
                    sharedType,
                    key,
                    item);
            };
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnInitialize.Order = -1;

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestStart += async (_, context) =>
            {
                await Get(dataGeneratorMetadata.TestSessionId).OnTestStart(context, item);
            };
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestStart.Order = -1;


            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestEnd += async (_, context) =>
            {
                await Get(dataGeneratorMetadata.TestSessionId).OnTestEnd(context, item);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestSkipped += async (_, context) =>
            {
                await Get(dataGeneratorMetadata.TestSessionId).OnDispose(context, sharedType, key, item);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnDispose += async (_, context) =>
            {
                await Get(dataGeneratorMetadata.TestSessionId).OnDispose(context, sharedType, key, item);
            };
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
}
