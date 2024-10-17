using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T1, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T2, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T3> : DataSourceGeneratorAttribute<T1, T2, T3>, ITestRegisteredEvents, ITestStartEvent, ITestEndEvent, ILastTestInClassEvent, ILastTestInAssemblyEvent 
    where T1 : new()
    where T2 : new()
    where T3 : new()
{
    private T1? _item1;
    private T2? _item2;
    private T3? _item3;
    private DataGeneratorMetadata? _dataGeneratorMetadata;
    
    public SharedType Shared { get; set; } = SharedType.None;
    public string Key { get; set; } = string.Empty;
    public override IEnumerable<(T1, T2, T3)> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        _dataGeneratorMetadata = dataGeneratorMetadata;

        var t = Shared switch
        {
            SharedType.None => (
                new T1(),
                new T2(),
                new T3()
            ),
            SharedType.Globally => (
                TestDataContainer.GetGlobalInstance(() => new T1()),
                TestDataContainer.GetGlobalInstance(() => new T2()),
                TestDataContainer.GetGlobalInstance(() => new T3())
            ),
            SharedType.ForClass => (
                TestDataContainer.GetInstanceForType<T1>(dataGeneratorMetadata.TestClassType, () => new T1()),
                TestDataContainer.GetInstanceForType<T2>(dataGeneratorMetadata.TestClassType, () => new T2()),
                TestDataContainer.GetInstanceForType<T3>(dataGeneratorMetadata.TestClassType, () => new T3())
            ),
            SharedType.Keyed => (
                TestDataContainer.GetInstanceForKey(Key, () => new T1()),
                TestDataContainer.GetInstanceForKey(Key, () => new T2()),
                TestDataContainer.GetInstanceForKey(Key, () => new T3())
            ),
            SharedType.ForAssembly => (
                TestDataContainer.GetInstanceForAssembly(dataGeneratorMetadata.TestClassType.Assembly, () => new T1()),
                TestDataContainer.GetInstanceForAssembly(dataGeneratorMetadata.TestClassType.Assembly, () => new T2()),
                TestDataContainer.GetInstanceForAssembly(dataGeneratorMetadata.TestClassType.Assembly, () => new T3())
            ),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        _item1 = t.Item1;
        _item2 = t.Item2;
        _item3 = t.Item3;
        
        yield return t;
    }

    public async ValueTask OnTestRegistered(TestContext testContext)
    {
        await ClassDataSources.OnTestRegistered<T1>(
            testContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key);
        
        await ClassDataSources.OnTestRegistered<T2>(
            testContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key);
        
        await ClassDataSources.OnTestRegistered<T3>(
            testContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key);
    }

    public async ValueTask OnTestStart(BeforeTestContext beforeTestContext)
    {
        await ClassDataSources.OnTestStart(
            beforeTestContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item1);
        
        await ClassDataSources.OnTestStart(
            beforeTestContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item2);
        
        await ClassDataSources.OnTestStart(
            beforeTestContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item3);
    }

    public async ValueTask OnTestEnd(TestContext testContext)
    {
        await ClassDataSources.OnTestEnd(Shared, Key, _item1);
        await ClassDataSources.OnTestEnd(Shared, Key, _item2);
        await ClassDataSources.OnTestEnd(Shared, Key, _item3);
    }

    public async ValueTask IfLastTestInClass(ClassHookContext context, TestContext testContext)
    {
        await ClassDataSources.IfLastTestInClass<T1>(Shared);
        await ClassDataSources.IfLastTestInClass<T2>(Shared);
        await ClassDataSources.IfLastTestInClass<T3>(Shared);
    }

    public async ValueTask IfLastTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        await ClassDataSources.IfLastTestInAssembly<T1>(Shared);
        await ClassDataSources.IfLastTestInClass<T2>(Shared);
        await ClassDataSources.IfLastTestInClass<T3>(Shared);
    }
}