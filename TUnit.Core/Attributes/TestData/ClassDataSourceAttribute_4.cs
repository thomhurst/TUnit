namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<
    T1, 
    T2, 
    T3, 
    T4>
    : DataSourceGeneratorAttribute<T1, T2, T3, T4> 
    where T1 : new()
    where T2 : new()
    where T3 : new()
    where T4 : new()
{
    public SharedType[] Shared { get; set; } = [SharedType.None, SharedType.None, SharedType.None, SharedType.None, SharedType.None];
    public string[] Keys { get; set; } = [string.Empty, string.Empty, string.Empty, string.Empty, string.Empty];
    
    public override IEnumerable<Func<(T1, T2, T3, T4)>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () =>
        {
            (
                (T1 T, SharedType SharedType, string Key),
                (T2 T, SharedType SharedType, string Key),
                (T3 T, SharedType SharedType, string Key),
                (T4 T, SharedType SharedType, string Key)
                ) itemsWithMetadata = (
                    ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                        .GetItemForIndex<T1>(0, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata),
                    ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                        .GetItemForIndex<T2>(1, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata),
                    ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                        .GetItemForIndex<T3>(2, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata),
                    ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                        .GetItemForIndex<T4>(3, dataGeneratorMetadata.TestClassType, Shared, Keys, dataGeneratorMetadata)
                );

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestRegistered += async (obj, context) =>
            {
                var testContext = context.TestContext;

                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestRegistered(
                    testContext,
                    false,
                    itemsWithMetadata.Item1.SharedType,
                    itemsWithMetadata.Item1.Key,
                    itemsWithMetadata.Item1.T);

                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestRegistered(
                    testContext,
                    false,
                    itemsWithMetadata.Item2.SharedType,
                    itemsWithMetadata.Item2.Key,
                    itemsWithMetadata.Item2.T);

                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestRegistered(
                    testContext,
                    false,
                    itemsWithMetadata.Item3.SharedType,
                    itemsWithMetadata.Item3.Key,
                    itemsWithMetadata.Item3.T);

                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestRegistered(
                    testContext,
                    false,
                    itemsWithMetadata.Item4.SharedType,
                    itemsWithMetadata.Item4.Key,
                    itemsWithMetadata.Item4.T);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnInitialize += async (obj, context) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnInitialize(
                    context,
                    false,
                    itemsWithMetadata.Item1.SharedType,
                    itemsWithMetadata.Item1.Key,
                    itemsWithMetadata.Item1.T);

                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnInitialize(
                    context,
                    false,
                    itemsWithMetadata.Item2.SharedType,
                    itemsWithMetadata.Item2.Key,
                    itemsWithMetadata.Item2.T);

                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnInitialize(
                    context,
                    false,
                    itemsWithMetadata.Item3.SharedType,
                    itemsWithMetadata.Item3.Key,
                    itemsWithMetadata.Item3.T);

                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnInitialize(
                    context,
                    false,
                    itemsWithMetadata.Item4.SharedType,
                    itemsWithMetadata.Item4.Key,
                    itemsWithMetadata.Item4.T);
            };
            
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestStart += async (obj, context) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestStart(context, itemsWithMetadata.Item1.T);
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestStart(context, itemsWithMetadata.Item2.T);
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestStart(context, itemsWithMetadata.Item2.T);
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestStart(context, itemsWithMetadata.Item4.T);
            };
            
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestEnd += async (obj, context) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestEnd(context, itemsWithMetadata.Item1.T);
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestEnd(context, itemsWithMetadata.Item2.T);
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestEnd(context, itemsWithMetadata.Item3.T);
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnTestEnd(context, itemsWithMetadata.Item4.T);
            };

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnTestSkipped += async (obj, context) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnDispose(
                    context,
                    itemsWithMetadata.Item1.SharedType,
                    itemsWithMetadata.Item1.Key,
                    itemsWithMetadata.Item1.T);

                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnDispose(
                    context,
                    itemsWithMetadata.Item2.SharedType,
                    itemsWithMetadata.Item2.Key,
                    itemsWithMetadata.Item2.T);

                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnDispose(
                    context,
                    itemsWithMetadata.Item3.SharedType,
                    itemsWithMetadata.Item3.Key,
                    itemsWithMetadata.Item3.T);

                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnDispose(
                    context,
                    itemsWithMetadata.Item4.SharedType,
                    itemsWithMetadata.Item4.Key,
                    itemsWithMetadata.Item4.T);
            };
            
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnDispose += async (obj, context) =>
            {
                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnDispose(
                    context,
                    itemsWithMetadata.Item1.SharedType,
                    itemsWithMetadata.Item1.Key,
                    itemsWithMetadata.Item1.T);

                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnDispose(
                    context,
                    itemsWithMetadata.Item2.SharedType,
                    itemsWithMetadata.Item2.Key,
                    itemsWithMetadata.Item2.T);

                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnDispose(
                    context,
                    itemsWithMetadata.Item3.SharedType,
                    itemsWithMetadata.Item3.Key,
                    itemsWithMetadata.Item3.T);

                await ClassDataSources.Get(dataGeneratorMetadata.TestSessionId).OnDispose(
                    context,
                    itemsWithMetadata.Item4.SharedType,
                    itemsWithMetadata.Item4.Key,
                    itemsWithMetadata.Item4.T);
            };

            return (
                itemsWithMetadata.Item1.T,
                itemsWithMetadata.Item2.T,
                itemsWithMetadata.Item3.T,
                itemsWithMetadata.Item4.T
            );
        };
    }
}