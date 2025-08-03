﻿using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] T1,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] T2,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] T3,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] T4,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] T5>
    : DataSourceGeneratorAttribute<T1, T2, T3, T4, T5>
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
            var testClassType = TestClassTypeHelper.GetTestClassType(dataGeneratorMetadata);
            var item1 = ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                .GetItemForIndexAsync<T1>(0, testClassType, Shared, Keys, dataGeneratorMetadata);
            var item2 = ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                .GetItemForIndexAsync<T2>(1, testClassType, Shared, Keys, dataGeneratorMetadata);
            var item3 = ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                .GetItemForIndexAsync<T3>(2, testClassType, Shared, Keys, dataGeneratorMetadata);
            var item4 = ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                .GetItemForIndexAsync<T4>(3, testClassType, Shared, Keys, dataGeneratorMetadata);
            var item5 = ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                .GetItemForIndexAsync<T5>(4, testClassType, Shared, Keys, dataGeneratorMetadata);

            return (item1.Item1, item2.Item1, item3.Item1, item4.Item1, item5.Item1);
        };
    }

    public IEnumerable<SharedType> GetSharedTypes() => Shared;

    public IEnumerable<string> GetKeys() => Keys;
}
