﻿using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T1, 
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T2>
    : DataSourceGeneratorAttribute<T1, T2> 
    where T1 : new()
    where T2 : new()
{
    private DataGeneratorMetadata? _dataGeneratorMetadata;
    
    public SharedType[] Shared { get; set; } = [SharedType.None, SharedType.None, SharedType.None];
    public string[] Keys { get; set; } = [string.Empty, string.Empty, string.Empty];

    private
    (
        (T1 T, SharedType SharedType, string Key),
        (T2 T, SharedType SharedType, string Key)
    ) _itemsWithMetadata;
    
    public override IEnumerable<(T1, T2)> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        _dataGeneratorMetadata = dataGeneratorMetadata;

        _itemsWithMetadata = (
            ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).GetItemForIndex<T1>(0, dataGeneratorMetadata.TestClassType, Shared, Keys),
            ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).GetItemForIndex<T2>(1, dataGeneratorMetadata.TestClassType, Shared, Keys)
        );

        dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestRegistered += async (_, context) =>
        {
            var testContext = context.TestContext;

            await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestRegistered(
                testContext,
                false,
                _itemsWithMetadata.Item1.SharedType,
                _itemsWithMetadata.Item1.Key,
                _itemsWithMetadata.Item1.T);

            await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestRegistered(
                testContext,
                false,
                _itemsWithMetadata.Item2.SharedType,
                _itemsWithMetadata.Item2.Key,
                _itemsWithMetadata.Item2.T);
        };

        dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestStart += async (_, context) =>
        {
            await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestStart(
                context,
                false,
                _itemsWithMetadata.Item1.SharedType,
                _itemsWithMetadata.Item1.Key,
                _itemsWithMetadata.Item1.T);

            await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestStart(
                context,
                false,
                _itemsWithMetadata.Item2.SharedType,
                _itemsWithMetadata.Item2.Key,
                _itemsWithMetadata.Item2.T);
        };

        dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestEnd += async (_, _) =>
        {
            await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                _itemsWithMetadata.Item1.SharedType,
                _itemsWithMetadata.Item1.Key,
                _itemsWithMetadata.Item1.T);

            await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                _itemsWithMetadata.Item2.SharedType,
                _itemsWithMetadata.Item2.Key,
                _itemsWithMetadata.Item2.T);
        };

        dataGeneratorMetadata.TestBuilderContext.Current.Events.OnLastTestInClass += async (_, _) =>
        {
            await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                .IfLastTestInClass<T1>(_itemsWithMetadata.Item1.SharedType);
            await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                .IfLastTestInClass<T2>(_itemsWithMetadata.Item2.SharedType);
        };

        dataGeneratorMetadata.TestBuilderContext.Current.Events.OnLastTestInAssembly += async (_, _) =>
        {
            await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                .IfLastTestInAssembly<T1>(_itemsWithMetadata.Item1.SharedType);
            await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                .IfLastTestInAssembly<T2>(_itemsWithMetadata.Item2.SharedType);
        };

        yield return 
        (
            _itemsWithMetadata.Item1.T,
            _itemsWithMetadata.Item2.T
        );
    }
}