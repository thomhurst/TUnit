using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : DataSourceGeneratorAttribute<T>, ITestRegisteredEvents, ITestEndEvents where T : new()
{
    private readonly List<object?> _toDisposeOnTestEnd = new();
    
    public ClassDataSourceAttribute()
    {
        if (!typeof(T).GetConstructors().Any(x => x.IsPublic && x.GetParameters().Length == 0))
        {
            throw new ArgumentException($"{typeof(T).FullName} cannot be used within [ClassData] as it does not have a public constructor.");
        }
    }
    
    public SharedType Shared { get; set; } = SharedType.None;
    public string Key { get; set; } = string.Empty;
    public override IEnumerable<T> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var t = Shared switch
        {
            SharedType.None => new T(),
            SharedType.Globally => TestDataContainer.GetGlobalInstance(() => new T()),
            SharedType.ForClass => TestDataContainer.GetInstanceForType<T>(dataGeneratorMetadata.TestClassType, () => new T()),
            SharedType.Keyed => TestDataContainer.GetInstanceForKey(Key, () => new T()),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        if(Shared == SharedType.None)
        {
            _toDisposeOnTestEnd.Add(t);
        }

        yield return t;
    }

    public Task OnTestRegistered(TestContext testContext)
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

        return Task.CompletedTask;
    }

    public async Task OnTestEnd(TestContext testContext)
    {
        if (Shared == SharedType.None)
        {
            foreach (T? obj in _toDisposeOnTestEnd)
            {
                await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(obj);
            }
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
}

public enum SharedType
{
    None,
    ForClass,
    ForAssembly,
    Globally,
    Keyed,
}