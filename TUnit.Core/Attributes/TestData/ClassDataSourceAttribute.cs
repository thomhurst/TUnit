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
        
        _item = ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).Get<T>(Shared, dataGeneratorMetadata.TestClassType, Key);

        yield return _item;
    }

    public async ValueTask OnTestRegistered(TestRegisterContext testRegisterContext)
    {
        await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestRegistered(
            testRegisterContext.TestContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item);
    }

    public ValueTask OnTestStart(BeforeTestContext beforeTestContext)
    {
        return ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestStart(
            beforeTestContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item);
    }

    public async ValueTask OnTestEnd(TestContext testContext)
    {
        await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(Shared, Key, _item);
    }

    public async ValueTask IfLastTestInClass(ClassHookContext context, TestContext testContext)
    {
        await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).IfLastTestInClass<T>(Shared);
    }

    public async ValueTask IfLastTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).IfLastTestInAssembly<T>(Shared);
    }
}