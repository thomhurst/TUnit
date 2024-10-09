using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Data;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : DataSourceGeneratorAttribute<T>, ITestRegisteredEvents, ITestStartEvents, ITestEndEvents where T : new()
{
    private T? _item;
    private DataGeneratorMetadata? _dataGeneratorMetadata;
    private static Task? GlobalInitializer;
    private static readonly GetOnlyDictionary<Type, Task> TestClassTypeInitializers = new();
    private static readonly GetOnlyDictionary<Assembly, Task> AssemblyInitializers = new();
    private static readonly GetOnlyDictionary<string, Task> KeyedInitializers = new();
    
    public SharedType Shared { get; set; } = SharedType.None;
    public string Key { get; set; } = string.Empty;
    public override IEnumerable<T> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        _dataGeneratorMetadata = dataGeneratorMetadata;

        var t = Shared switch
        {
            SharedType.None => new T(),
            SharedType.Globally => TestDataContainer.GetGlobalInstance(() => new T()),
            SharedType.ForClass => TestDataContainer.GetInstanceForType<T>(dataGeneratorMetadata.TestClassType, () => new T()),
            SharedType.Keyed => TestDataContainer.GetInstanceForKey(Key, () => new T()),
            SharedType.ForAssembly => TestDataContainer.GetInstanceForAssembly(dataGeneratorMetadata.TestClassType.Assembly, () => new T()),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        _item = t;

        yield return t;
    }

    public async Task OnTestRegistered(TestContext testContext)
    {
        switch (Shared)
        {
            case SharedType.None:
                break;
            case SharedType.ForClass:
                break;
            case SharedType.Globally:
                TestDataContainer.IncrementGlobalUsage(typeof(T));
                break;
            case SharedType.Keyed:
                TestDataContainer.IncrementKeyUsage(Key, typeof(T));
                break;
            case SharedType.ForAssembly:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (_dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true)
        {
            await Initialize(testContext);
        }
    }

    public async Task OnTestStart(TestContext testContext)
    {
        if (_dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true)
        {
            // Done already before test start
            return;
        }
        
        await Initialize(testContext);
    }

    private Task Initialize(TestContext testContext)
    {
        return Shared switch
        {
            SharedType.Globally => GlobalInitializer ??= Initialize(_item),
            SharedType.None => Initialize(_item),
            SharedType.ForClass => TestClassTypeInitializers.GetOrAdd(testContext.TestDetails.ClassType,
                _ => Initialize(_item)),
            SharedType.ForAssembly => AssemblyInitializers.GetOrAdd(testContext.TestDetails.ClassType.Assembly,
                _ => Initialize(_item)),
            SharedType.Keyed => KeyedInitializers.GetOrAdd(Key, _ => Initialize(_item)),
            _ => throw new ArgumentOutOfRangeException(nameof(Shared))
        };
    }

    public async Task OnTestEnd(TestContext testContext)
    {
        if (Shared == SharedType.None)
        {
            await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(_item);
        }

        if (Shared == SharedType.Keyed)
        {
            await TestDataContainer.ConsumeKey(Key, typeof(T));
        }

        if (Shared == SharedType.Globally)
        {
            await TestDataContainer.ConsumeGlobalCount(typeof(T));
        }
    }

    public async Task IfLastTestInClass(ClassHookContext context, TestContext testContext)
    {
        if (Shared == SharedType.ForClass)
        {
            await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(TestDataContainer.GetInstanceForType(typeof(T), () => default(T)!));
        }
    }

    public async Task IfLastTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        if (Shared == SharedType.ForAssembly)
        {
            await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(TestDataContainer.GetInstanceForType(typeof(T), () => default(T)!));
        }
    }

    public Task IfLastTestInTestSession(TestSessionContext current, TestContext testContext)
    {
        return Task.CompletedTask;
    }

    private Task Initialize(T? item)
    {
        if (item is IAsyncInitializer asyncInitializer)
        {
            return asyncInitializer.InitializeAsync();
        }
        
        return Task.CompletedTask;
    }
}

public enum SharedType
{
    None,
    ForClass,
    ForAssembly,
    Globally,
    Keyed,
}