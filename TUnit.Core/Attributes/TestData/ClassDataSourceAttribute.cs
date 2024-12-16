﻿using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : DataSourceGeneratorAttribute<T> where T : new()
{
    public SharedType Shared { get; set; } = SharedType.None;
    public string Key { get; set; } = string.Empty;
    public override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () =>
        {
            var item = ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId)
                .Get<T>(Shared, dataGeneratorMetadata.TestClassType, Key);

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestRegistered += async (_, context) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnTestRegistered(
                    context.TestContext,
                    dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
                    Shared,
                    Key,
                    item);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestStart += async (_, context) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnTestStart(
                    context,
                    dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
                    Shared,
                    Key,
                    item);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestSkipped += async (_, context) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnTestEnd(Shared, Key, item);
            };
            
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestEnd += async (_, context) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnTestEnd(Shared, Key, item);
            };

            if (Shared is SharedType.PerClass)
            {
                dataGeneratorMetadata.TestBuilderContext.Current.Events.OnLastTestInClass += async (_, context) =>
                {
                    await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnLastTestInClass<T>(context.Item2);
                };
            }

            if (Shared is SharedType.PerAssembly)
            {
                dataGeneratorMetadata.TestBuilderContext.Current.Events.OnLastTestInAssembly += async (_, context) =>
                {
                    await ClassDataSources.Get(dataGeneratorMetadata!.TestSessionId).OnLastTestInAssembly<T>(context.Item2);
                };
            }

            return item;
        };
    }
}