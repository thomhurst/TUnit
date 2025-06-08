using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T2>
    : DataSourceGeneratorAttribute<T1, T2>, ISharedDataSourceAttribute
    where T1 : new()
    where T2 : new()
{
    public SharedType[] Shared { get; set; } = [SharedType.None, SharedType.None, SharedType.None, SharedType.None, SharedType.None];
    public string[] Keys { get; set; } = [string.Empty, string.Empty, string.Empty, string.Empty, string.Empty];

    public override IEnumerable<Func<(T1, T2)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () =>
        {
            (
                (T1 T, SharedType SharedType, string Key),
                (T2 T, SharedType SharedType, string Key)
                ) itemsWithMetadata = (
                    ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                        .GetItemForIndex<T1>(0, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata),
                    ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                        .GetItemForIndex<T2>(1, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata)
                );

            return (
                itemsWithMetadata.Item1.T,
                itemsWithMetadata.Item2.T
            );
        };
    }

    public IEnumerable<SharedType> GetSharedTypes() => Shared;

    public IEnumerable<string> GetKeys() => Keys;
}
