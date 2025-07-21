namespace TUnit.Core.DataSourceGenerators;

/// <summary>
/// Generates TestDataCombination objects for AsyncDataSourceGeneratorAttribute.
/// Handles async data sources by awaiting the async enumerable and converting results.
/// </summary>
public class AsyncDataSourceGenerator : IAsyncDataSourceGenerator
{
    public async IAsyncEnumerable<TestDataCombination> GenerateDataCombinationsAsync(IDataSourceAttribute attribute, DataSourceGenerationContext context)
    {
        var asyncEnumerable = GetAsyncEnumerable(attribute, context);
        await foreach (var combination in ConvertAsyncEnumerableToDataCombinationsAsync(asyncEnumerable, context.DataSourceIndex))
        {
            yield return combination;
        }
    }

    private static IAsyncEnumerable<Func<Task<object?[]?>>> GetAsyncEnumerable(IDataSourceAttribute attribute, DataSourceGenerationContext context)
    {
        // For now, use a minimal DataGeneratorMetadata - this will need proper implementation
        // when integrating with the full TUnit runtime pipeline
        throw new NotImplementedException("Async data source generators require full runtime context and should be handled by the actual TUnit runtime, not these simplified generators");
    }

    private static async IAsyncEnumerable<TestDataCombination> ConvertAsyncEnumerableToDataCombinationsAsync(IAsyncEnumerable<Func<Task<object?[]?>>> asyncEnumerable, int dataSourceIndex)
    {
        var loopIndex = 0;

        await foreach (var factoryFunc in asyncEnumerable)
        {
            if (factoryFunc != null)
            {
                // Get initial data to determine array length
                var initialData = await factoryFunc();
                var dataLength = initialData?.Length ?? 0;

                yield return new TestDataCombination
                {
                    MethodDataFactories = Enumerable.Range(0, dataLength).Select(index => new Func<Task<object?>>(async () => (await factoryFunc())?[index])).ToArray(),
                    ClassDataFactories = [
                    ],
                    MethodDataSourceIndex = dataSourceIndex,
                    MethodLoopIndex = loopIndex,
                    ClassDataSourceIndex = -1,
                    ClassLoopIndex = 0
                };
                loopIndex++;
            }
        }
    }
}