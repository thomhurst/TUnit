using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : DataSourceGeneratorAttribute<T> where T : new()
{
    private DataGeneratorMetadata? _dataGeneratorMetadata;
    
    public SharedType Shared { get; set; } = SharedType.None;
    public string Key { get; set; } = string.Empty;
    public override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        _dataGeneratorMetadata = dataGeneratorMetadata;

        yield return () =>
        {
            var item = ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                .Get<T>(Shared, dataGeneratorMetadata.TestClassType, Key);

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestRegistered += async (_, context) =>
            {
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestRegistered(
                    context.TestContext,
                    _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
                    Shared,
                    Key,
                    item);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestStart += async (_, context) =>
            {
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestStart(
                    context,
                    _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
                    Shared,
                    Key,
                    item);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestEnd += async (_, context) =>
            {
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(Shared, Key, item);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnLastTestInClass += async (_, context) =>
            {
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).IfLastTestInClass<T>(Shared);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnLastTestInAssembly += async (_, context) =>
            {
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).IfLastTestInAssembly<T>(Shared);
            };

            return item;
        };
    }
}