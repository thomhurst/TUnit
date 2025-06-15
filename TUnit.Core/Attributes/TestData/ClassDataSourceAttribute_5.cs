using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T2,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T3,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T4,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T5>
    : DataSourceGeneratorAttribute<T1, T2, T3, T4, T5>, ISharedDataSourceAttribute
    where T1 : new()
    where T2 : new()
    where T3 : new()
    where T4 : new()
    where T5 : new()
{
    public SharedType[] Shared { get; set; } = [SharedType.None, SharedType.None, SharedType.None, SharedType.None, SharedType.None];
    public string[] Keys { get; set; } = [string.Empty, string.Empty, string.Empty, string.Empty, string.Empty];

    protected override IEnumerable<Func<(T1, T2, T3, T4, T5)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () =>
        {
            (
                (T1 T, SharedType SharedType, string Key),
                (T2 T, SharedType SharedType, string Key),
                (T3 T, SharedType SharedType, string Key),
                (T4 T, SharedType SharedType, string Key),
                (T5 T, SharedType SharedType, string Key)
                ) itemsWithMetadata = (
                    ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                        .GetItemForIndex<T1>(0, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata),
                    ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                        .GetItemForIndex<T2>(1, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata),
                    ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                        .GetItemForIndex<T3>(2, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata),
                    ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                        .GetItemForIndex<T4>(3, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata),
                    ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                        .GetItemForIndex<T5>(4, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata)
                );

            return (
                itemsWithMetadata.Item1.T,
                itemsWithMetadata.Item2.T,
                itemsWithMetadata.Item3.T,
                itemsWithMetadata.Item4.T,
                itemsWithMetadata.Item5.T
            );
        };
    }

    public IEnumerable<SharedType> GetSharedTypes() => Shared;

    public IEnumerable<string> GetKeys() => Keys;
}
