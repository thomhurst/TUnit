using TUnit.Core;

namespace TUnit.Core.SourceGenerator.DataSourceGenerators;

/// <summary>
/// Generates TestDataCombination objects for ArgumentsAttribute.
/// Handles compile-time known values by wrapping them in TestDataCombination objects.
/// </summary>
public class ArgumentsDataSourceGenerator : IDataSourceGenerator<ArgumentsAttribute>
{
    public IEnumerable<TestDataCombination> GenerateDataCombinations(ArgumentsAttribute attribute, DataSourceGenerationContext context)
    {
        yield return new TestDataCombination
        {
            MethodData = attribute.Values,
            ClassData = Array.Empty<object?>(),
            DataSourceIndices = new[] { context.DataSourceIndex },
            PropertyValues = new Dictionary<string, object?>()
        };
    }
}