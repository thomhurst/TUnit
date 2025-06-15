using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T2,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T3,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T4,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T5>
    : AsyncDataSourceGeneratorAttribute<T1, T2, T3, T4, T5>, ISharedDataSourceAttribute
    where T1 : new()
    where T2 : new()
    where T3 : new()
    where T4 : new()
    where T5 : new()
{
    public SharedType[] Shared { get; set; } = [SharedType.None, SharedType.None, SharedType.None, SharedType.None, SharedType.None];
    public string[] Keys { get; set; } = [string.Empty, string.Empty, string.Empty, string.Empty, string.Empty];

    protected override async IAsyncEnumerable<Func<Task<(T1, T2, T3, T4, T5)>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await Task.CompletedTask;
        yield return async () =>
        {
            var item1 = await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                .GetItemForIndexAsync<T1>(0, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata);
            var item2 = await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                .GetItemForIndexAsync<T2>(1, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata);
            var item3 = await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                .GetItemForIndexAsync<T3>(2, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata);
            var item4 = await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                .GetItemForIndexAsync<T4>(3, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata);
            var item5 = await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                .GetItemForIndexAsync<T5>(4, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata);

            return (item1.Item1, item2.Item1, item3.Item1, item4.Item1, item5.Item1);
        };
    }

    public IEnumerable<SharedType> GetSharedTypes() => Shared;

    public IEnumerable<string> GetKeys() => Keys;
}
