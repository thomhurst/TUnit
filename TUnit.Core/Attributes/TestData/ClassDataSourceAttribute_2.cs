using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] T2>
    : DataSourceGeneratorAttribute<T1, T2>
    where T1 : new()
    where T2 : new()
{
    public SharedType[] Shared { get; set; } = [SharedType.None, SharedType.None, SharedType.None, SharedType.None, SharedType.None];
    public string[] Keys { get; set; } = [string.Empty, string.Empty, string.Empty, string.Empty, string.Empty];

    protected override IEnumerable<Func<(T1, T2)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () =>
        {
            var item1 = ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                .GetItemForIndexAsync<T1>(0, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata);
            var item2 = ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                .GetItemForIndexAsync<T2>(1, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata);

            return (item1.Item1, item2.Item1);
        };
    }

    public IEnumerable<SharedType> GetSharedTypes() => Shared;

    public IEnumerable<string> GetKeys() => Keys;
}
