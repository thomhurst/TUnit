﻿using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T1, 
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T2>
    : DataSourceGeneratorAttribute<T1, T2> 
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
                    ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId)
                        .GetItemForIndex<T1>(0, dataGeneratorMetadata.TestClassType, Shared, Keys),
                    ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId)
                        .GetItemForIndex<T2>(1, dataGeneratorMetadata.TestClassType, Shared, Keys)
                );

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestRegistered += async (_, context) =>
            {
                var testContext = context.TestContext;

                await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnTestRegistered(
                    testContext,
                    false,
                    itemsWithMetadata.Item1.SharedType,
                    itemsWithMetadata.Item1.Key,
                    itemsWithMetadata.Item1.T);

                await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnTestRegistered(
                    testContext,
                    false,
                    itemsWithMetadata.Item2.SharedType,
                    itemsWithMetadata.Item2.Key,
                    itemsWithMetadata.Item2.T);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestStart += async (_, context) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnTestStart(
                    context,
                    false,
                    itemsWithMetadata.Item1.SharedType,
                    itemsWithMetadata.Item1.Key,
                    itemsWithMetadata.Item1.T);

                await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnTestStart(
                    context,
                    false,
                    itemsWithMetadata.Item2.SharedType,
                    itemsWithMetadata.Item2.Key,
                    itemsWithMetadata.Item2.T);
            };
            
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestSkipped += async (_, _) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item1.SharedType,
                    itemsWithMetadata.Item1.Key,
                    itemsWithMetadata.Item1.T);

                await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item2.SharedType,
                    itemsWithMetadata.Item2.Key,
                    itemsWithMetadata.Item2.T);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestEnd += async (_, _) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item1.SharedType,
                    itemsWithMetadata.Item1.Key,
                    itemsWithMetadata.Item1.T);

                await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnTestEnd(
                    itemsWithMetadata.Item2.SharedType,
                    itemsWithMetadata.Item2.Key,
                    itemsWithMetadata.Item2.T);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnLastTestInClass += async (_, context) =>
            {
                if (Shared.ElementAtOrDefault(0) is SharedType.PerClass)
                {
                    await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnLastTestInClass<T1>(context.Item2);
                }

                if (Shared.ElementAtOrDefault(1) is SharedType.PerClass)
                {
                    await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnLastTestInClass<T2>(context.Item2);
                }
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnLastTestInAssembly += async (_, context) =>
            {
                if (Shared.ElementAtOrDefault(0) is SharedType.PerAssembly)
                {
                    await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId)
                        .OnLastTestInAssembly<T1>(context.Item2);
                }

                if (Shared.ElementAtOrDefault(1) is SharedType.PerAssembly)
                {
                    await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId)
                        .OnLastTestInAssembly<T2>(context.Item2);
                }
            };

            return (
                itemsWithMetadata.Item1.T,
                itemsWithMetadata.Item2.T
            );
        };
    }
}