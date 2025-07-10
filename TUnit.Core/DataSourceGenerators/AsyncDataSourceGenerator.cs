using System.Reflection;
using TUnit.Core;

namespace TUnit.Core.DataSourceGenerators;

/// <summary>
/// Generates TestDataCombination objects for AsyncDataSourceGeneratorAttribute.
/// Handles async data sources by awaiting the async enumerable and converting results.
/// </summary>
public class AsyncDataSourceGenerator : IAsyncDataSourceGenerator
{
    public async IAsyncEnumerable<TestDataCombination> GenerateDataCombinationsAsync(IAsyncDataSourceGeneratorAttribute attribute, DataSourceGenerationContext context)
    {
        var asyncEnumerable = GetAsyncEnumerable(attribute, context);
        await foreach (var combination in ConvertAsyncEnumerableToDataCombinationsAsync(asyncEnumerable, context.DataSourceIndex))
        {
            yield return combination;
        }
    }

    private static IAsyncEnumerable<Func<Task<object?[]?>>> GetAsyncEnumerable(IAsyncDataSourceGeneratorAttribute attribute, DataSourceGenerationContext context)
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
                var methodData = await factoryFunc();

                yield return new TestDataCombination
                {
                    MethodDataFactories = (methodData ?? Array.Empty<object?>()).Select<object?, Func<object?>>(item => () => item).ToArray(),
                    ClassDataFactories = Array.Empty<Func<object?>>(),
                    MethodDataSourceIndex = dataSourceIndex,
                    MethodLoopIndex = loopIndex,
                    ClassDataSourceIndex = -1,
                    ClassLoopIndex = 0,
                    PropertyValueFactories = new Dictionary<string, Func<object?>>()
                };
                loopIndex++;
            }
        }
    }
}