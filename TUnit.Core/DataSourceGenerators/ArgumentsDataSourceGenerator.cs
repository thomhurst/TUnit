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
            MethodData = attribute.Values,
            ClassData = Array.Empty<object?>(),
            DataSourceIndices = new[] { context.DataSourceIndex },
            PropertyValues = new Dictionary<string, object?>()
        };
    }
}