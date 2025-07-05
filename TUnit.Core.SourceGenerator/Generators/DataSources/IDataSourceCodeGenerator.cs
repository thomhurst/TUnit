using TUnit.Core.SourceGenerator;

namespace TUnit.Core.SourceGenerator.Generators.DataSources;

/// <summary>
/// Interface for generating code for data sources.
/// Implementations of this interface are responsible for generating the appropriate
/// TestDataSource subclass instances (StaticTestDataSource, DelegateDataSource, etc.)
/// based on the extracted data source information.
/// </summary>
public interface IDataSourceCodeGenerator
{
    /// <summary>
    /// Generates code for a data source instance
    /// </summary>
    /// <param name="writer">The code writer to write to</param>
    /// <param name="dataSource">The extracted data source to generate code for</param>
    void GenerateDataSourceInstance(CodeWriter writer, ExtractedDataSource dataSource);
}