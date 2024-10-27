using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T1, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T2> : DataSourceGeneratorAttribute<T1, T2>, ITestRegisteredEvents, ITestStartEvent, ITestEndEvent, ILastTestInClassEvent, ILastTestInAssemblyEvent 
    where T1 : new()
    where T2 : new()
{
    private DataGeneratorMetadata? _dataGeneratorMetadata;
    
    public SharedType[] Shared { get; set; } = [SharedType.None, SharedType.None, SharedType.None, SharedType.None, SharedType.None];
    public string[] Keys { get; set; } = [string.Empty, string.Empty, string.Empty, string.Empty, string.Empty];

    private
    (
        (T1 T, SharedType SharedType, string Key),
        (T2 T, SharedType SharedType, string Key)
    ) _itemsWithMetadata;
    
    public override IEnumerable<(T1, T2)> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        _dataGeneratorMetadata = dataGeneratorMetadata;

        _itemsWithMetadata = 
        (
            ClassDataSources.GetItemForIndex<T1>(0, dataGeneratorMetadata.TestClassType, Shared, Keys),
            ClassDataSources.GetItemForIndex<T2>(1, dataGeneratorMetadata.TestClassType, Shared, Keys)
        );

        yield return 
        (
            _itemsWithMetadata.Item1.T,
            _itemsWithMetadata.Item2.T
        );
    }

    public async ValueTask OnTestRegistered(TestContext testContext)
    {
        await ClassDataSources.OnTestRegistered(
            testContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            _itemsWithMetadata.Item1.SharedType,
            _itemsWithMetadata.Item1.Key,
            _itemsWithMetadata.Item1.T);
        
        await ClassDataSources.OnTestRegistered(
            testContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            _itemsWithMetadata.Item2.SharedType,
            _itemsWithMetadata.Item2.Key,
            _itemsWithMetadata.Item2.T);
    }

    public async ValueTask OnTestStart(BeforeTestContext beforeTestContext)
    {
        await ClassDataSources.OnTestStart(
            beforeTestContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            _itemsWithMetadata.Item1.SharedType,
            _itemsWithMetadata.Item1.Key,
            _itemsWithMetadata.Item1.Key);
        
        await ClassDataSources.OnTestStart(
            beforeTestContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            _itemsWithMetadata.Item2.SharedType,
            _itemsWithMetadata.Item2.Key,
            _itemsWithMetadata.Item2.Key);
    }

    public async ValueTask OnTestEnd(TestContext testContext)
    {
        await ClassDataSources.OnTestEnd(
            _itemsWithMetadata.Item1.SharedType,
            _itemsWithMetadata.Item1.Key,
            _itemsWithMetadata.Item1.T);

        await ClassDataSources.OnTestEnd(
            _itemsWithMetadata.Item2.SharedType,
            _itemsWithMetadata.Item2.Key,
            _itemsWithMetadata.Item2.T);
    }

    public async ValueTask IfLastTestInClass(ClassHookContext context, TestContext testContext)
    {
        await ClassDataSources.IfLastTestInClass<T1>(_itemsWithMetadata.Item1.SharedType);
        await ClassDataSources.IfLastTestInClass<T2>(_itemsWithMetadata.Item2.SharedType);
    }

    public async ValueTask IfLastTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        await ClassDataSources.IfLastTestInAssembly<T1>(_itemsWithMetadata.Item1.SharedType);
        await ClassDataSources.IfLastTestInAssembly<T2>(_itemsWithMetadata.Item2.SharedType);
    }
}