﻿using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T1, 
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T2, 
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T3, 
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T4, 
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T5> 
    : DataSourceGeneratorAttribute<T1, T2, T3, T4, T5> 
    where T1 : new()
    where T2 : new()
    where T3 : new()
    where T4 : new()
    where T5 : new()
{
    private DataGeneratorMetadata? _dataGeneratorMetadata;
    
    public SharedType[] Shared { get; set; } = [SharedType.None, SharedType.None, SharedType.None, SharedType.None, SharedType.None];
    public string[] Keys { get; set; } = [string.Empty, string.Empty, string.Empty, string.Empty, string.Empty];
    
    public override IEnumerable<Func<(T1, T2, T3, T4, T5)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        _dataGeneratorMetadata = dataGeneratorMetadata;

        yield return () =>
        {
            (
                (T1 T, SharedType SharedType, string Key),
                (T2 T, SharedType SharedType, string Key),
                (T3 T, SharedType SharedType, string Key),
                (T4 T, SharedType SharedType, string Key),
                (T5 T, SharedType SharedType, string Key)
                ) itemsWithMetadata = (
                    ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                        .GetItemForIndex<T1>(0, dataGeneratorMetadata.TestClassType, Shared, Keys),
                    ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                        .GetItemForIndex<T2>(1, dataGeneratorMetadata.TestClassType, Shared, Keys),
                    ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                        .GetItemForIndex<T3>(2, dataGeneratorMetadata.TestClassType, Shared, Keys),
                    ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                        .GetItemForIndex<T4>(3, dataGeneratorMetadata.TestClassType, Shared, Keys),
                    ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                        .GetItemForIndex<T5>(4, dataGeneratorMetadata.TestClassType, Shared, Keys)
                );

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestRegistered += async (_, context) =>
            {
                var testContext = context.TestContext;

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestRegistered(
                    testContext,
                    false,
                    itemsWithMetadata.Item1.SharedType,
                    itemsWithMetadata.Item1.Key,
                    itemsWithMetadata.Item1.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestRegistered(
                    testContext,
                    false,
                    itemsWithMetadata.Item2.SharedType,
                    itemsWithMetadata.Item2.Key,
                    itemsWithMetadata.Item2.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestRegistered(
                    testContext,
                    false,
                    itemsWithMetadata.Item3.SharedType,
                    itemsWithMetadata.Item3.Key,
                    itemsWithMetadata.Item3.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestRegistered(
                    testContext,
                    false,
                    itemsWithMetadata.Item4.SharedType,
                    itemsWithMetadata.Item4.Key,
                    itemsWithMetadata.Item4.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestRegistered(
                    testContext,
                    false,
                    itemsWithMetadata.Item5.SharedType,
                    itemsWithMetadata.Item5.Key,
                    itemsWithMetadata.Item5.T);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestStart += async (_, context) =>
            {
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestStart(
                    context,
                    false,
                    itemsWithMetadata.Item1.SharedType,
                    itemsWithMetadata.Item1.Key,
                    itemsWithMetadata.Item1.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestStart(
                    context,
                    false,
                    itemsWithMetadata.Item2.SharedType,
                    itemsWithMetadata.Item2.Key,
                    itemsWithMetadata.Item2.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestStart(
                    context,
                    false,
                    itemsWithMetadata.Item3.SharedType,
                    itemsWithMetadata.Item3.Key,
                    itemsWithMetadata.Item3.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestStart(
                    context,
                    false,
                    itemsWithMetadata.Item4.SharedType,
                    itemsWithMetadata.Item4.Key,
                    itemsWithMetadata.Item4.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestStart(
                    context,
                    false,
                    itemsWithMetadata.Item5.SharedType,
                    itemsWithMetadata.Item5.Key,
                    itemsWithMetadata.Item5.T);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestSkipped += async (_, _) =>
            {
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item1.SharedType,
                    itemsWithMetadata.Item1.Key,
                    itemsWithMetadata.Item1.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item2.SharedType,
                    itemsWithMetadata.Item2.Key,
                    itemsWithMetadata.Item2.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item3.SharedType,
                    itemsWithMetadata.Item3.Key,
                    itemsWithMetadata.Item3.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item4.SharedType,
                    itemsWithMetadata.Item4.Key,
                    itemsWithMetadata.Item4.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item5.SharedType,
                    itemsWithMetadata.Item5.Key,
                    itemsWithMetadata.Item5.T);
            };
            
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestEnd += async (_, _) =>
            {
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item1.SharedType,
                    itemsWithMetadata.Item1.Key,
                    itemsWithMetadata.Item1.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item2.SharedType,
                    itemsWithMetadata.Item2.Key,
                    itemsWithMetadata.Item2.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item3.SharedType,
                    itemsWithMetadata.Item3.Key,
                    itemsWithMetadata.Item3.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item4.SharedType,
                    itemsWithMetadata.Item4.Key,
                    itemsWithMetadata.Item4.T);

                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item5.SharedType,
                    itemsWithMetadata.Item5.Key,
                    itemsWithMetadata.Item5.T);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnLastTestInClass += async (_, _) =>
            {
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                    .IfLastTestInClass<T1>(itemsWithMetadata.Item1.SharedType);
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                    .IfLastTestInClass<T2>(itemsWithMetadata.Item2.SharedType);
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                    .IfLastTestInClass<T3>(itemsWithMetadata.Item3.SharedType);
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                    .IfLastTestInClass<T4>(itemsWithMetadata.Item4.SharedType);
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                    .IfLastTestInClass<T5>(itemsWithMetadata.Item5.SharedType);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnLastTestInAssembly += async (_, _) =>
            {
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                    .IfLastTestInAssembly<T1>(itemsWithMetadata.Item1.SharedType);
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                    .IfLastTestInAssembly<T2>(itemsWithMetadata.Item2.SharedType);
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                    .IfLastTestInAssembly<T3>(itemsWithMetadata.Item3.SharedType);
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                    .IfLastTestInAssembly<T4>(itemsWithMetadata.Item4.SharedType);
                await ClassDataSources.Get(_dataGeneratorMetadata!.TestSessionId)
                    .IfLastTestInAssembly<T5>(itemsWithMetadata.Item5.SharedType);
            };

            return (
                itemsWithMetadata.Item1.T,
                itemsWithMetadata.Item2.T,
                itemsWithMetadata.Item3.T,
                itemsWithMetadata.Item4.T,
                itemsWithMetadata.Item5.T
            );
        };
    }
}