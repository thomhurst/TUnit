using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<T> : DataSourceGeneratorAttribute<T> where T : new()
{
    public SharedType Shared { get; set; } = SharedType.None;
    public string Key { get; set; } = string.Empty;
    public override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () =>
        {
            var item = ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                .Get<T>(Shared, dataGeneratorMetadata.TestClassType, Key, dataGeneratorMetadata);

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestRegistered += async (_, context) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestRegistered(
                    context.TestContext,
                    ClassDataSources.IsStaticProperty(dataGeneratorMetadata),
                    Shared,
                    Key,
                    item);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestStart += async (_, context) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestStart(
                    context,
                    ClassDataSources.IsStaticProperty(dataGeneratorMetadata),
                    Shared,
                    Key,
                    item);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestSkipped += async (_, _) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestEnd(Shared, Key, item);
            };
            
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestEnd += async (_, _) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestEnd(Shared, Key, item);
            };
            
            return item;
        };
    }
}