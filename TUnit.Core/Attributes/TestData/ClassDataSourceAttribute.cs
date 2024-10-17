using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : DataSourceGeneratorAttribute<T>, ITestRegisteredEvents, ITestStartEvent, ITestEndEvent, ILastTestInClassEvent, ILastTestInAssemblyEvent where T : new()
{
    private T? _item;
    private DataGeneratorMetadata? _dataGeneratorMetadata;
    
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

    public async ValueTask OnTestRegistered(TestContext testContext)
    {
        await ClassDataSources.OnTestRegistered<T>(
            testContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key);
    }

    public ValueTask OnTestStart(BeforeTestContext beforeTestContext)
    {
        return ClassDataSources.OnTestStart(
            beforeTestContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item);
    }

    private Task Initialize(TestContext testContext)
    {
        return ClassDataSources.Initialize(
            testContext,
            Shared,
            Key,
            _item);
    }

    public async ValueTask OnTestEnd(TestContext testContext)
    {
        await ClassDataSources.OnTestEnd<T>(Shared,
            Key, _item);
    }

    public async ValueTask IfLastTestInClass(ClassHookContext context, TestContext testContext)
    {
        await ClassDataSources.IfLastTestInClass<T>(Shared);
    }

    public async ValueTask IfLastTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        await ClassDataSources.IfLastTestInAssembly<T>(Shared);
    }
}