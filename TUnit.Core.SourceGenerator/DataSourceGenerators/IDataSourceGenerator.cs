using TUnit.Core;

namespace TUnit.Core.SourceGenerator.DataSourceGenerators;

/// <summary>
/// Interface for generating TestDataCombination objects from specific attribute types.
/// Each implementation handles a single type of data source attribute following SOLID principles.
/// </summary>
/// <typeparam name="TAttribute">The type of attribute this generator handles</typeparam>
public interface IDataSourceGenerator<TAttribute> where TAttribute : TestDataAttribute
{
    /// <summary>
    /// Generates TestDataCombination objects from the given attribute.
    /// This method should be implemented to produce all possible data combinations
    /// for the specific attribute type.
    /// </summary>
    /// <param name="attribute">The attribute instance containing the data source configuration</param>
    /// <param name="context">Context information about the test being generated</param>
    /// <returns>An enumerable of TestDataCombination objects</returns>
    IEnumerable<TestDataCombination> GenerateDataCombinations(TAttribute attribute, DataSourceGenerationContext context);
}

/// <summary>
/// Context information provided to data source generators
/// </summary>
public class DataSourceGenerationContext
{
    /// <summary>
    /// The type of the test class
    /// </summary>
    public required Type TestClassType { get; init; }

    /// <summary>
    /// The name of the test method
    /// </summary>
    public required string TestMethodName { get; init; }

    /// <summary>
    /// Parameter types for the test method
    /// </summary>
    public required Type[] ParameterTypes { get; init; }

    /// <summary>
    /// Index of this data source among all data sources for the test method.
    /// Used for generating deterministic test IDs.
    /// </summary>
    public required int DataSourceIndex { get; init; }
}