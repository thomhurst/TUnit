using TUnit.Core;

namespace TUnit.Core.DataSourceGenerators;

/// <summary>
/// Generates TestDataCombination objects for ArgumentsAttribute.
/// Handles compile-time known values by wrapping them in TestDataCombination objects.
/// </summary>
public class ArgumentsDataSourceGenerator : IDataSourceGenerator<ArgumentsAttribute>
{
    public async IAsyncEnumerable<TestDataCombination> GenerateDataCombinationsAsync(ArgumentsAttribute attribute, DataSourceGenerationContext context)
    {
        await Task.Yield(); // Make it properly async
        
        yield return new TestDataCombination
        {
            MethodDataFactories = attribute.Values.Select<object?, Func<Task<object?>>>(value => () => Task.FromResult(value)).ToArray(),
            ClassDataFactories = Array.Empty<Func<Task<object?>>>(),
            MethodDataSourceIndex = context.DataSourceIndex,
            MethodLoopIndex = 0, // ArgumentsAttribute only returns one row
            ClassDataSourceIndex = -1,
            ClassLoopIndex = 0,
            PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>()
        };
    }
}