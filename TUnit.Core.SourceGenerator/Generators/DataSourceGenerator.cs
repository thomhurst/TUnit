using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Generators.DataSources;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Responsible for generating data source factories and registrations
/// </summary>
public sealed class DataSourceGenerator
{
    private readonly IDataSourceExtractor _extractor;
    private readonly IDataSourceCodeGenerator _codeGenerator;

    public DataSourceGenerator()
    {
        _extractor = new UnifiedDataSourceExtractor();
        _codeGenerator = new UnifiedDataSourceCodeGenerator();
    }

    /// <summary>
    /// Generates data source factory registrations
    /// </summary>
    public void GenerateDataSourceRegistrations(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        // Data source registrations are no longer needed - using inline delegates instead
        // This method is kept for backward compatibility but does nothing
    }

    /// <summary>
    /// Generates async data source wrapper methods for all test methods
    /// </summary>
    public void GenerateAsyncDataSourceWrappers(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        // Async wrappers are no longer needed - using inline delegates instead
        // This method is kept for backward compatibility but does nothing
    }

    /// <summary>
    /// Generates data source metadata for a test method
    /// </summary>
    public void GenerateDataSourceMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        try
        {
            GenerateDataSourceMetadataForLevel(writer, testInfo, DataSourceLevel.Method, "DataSources");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate method data sources for {testInfo.TypeSymbol.Name}.{testInfo.MethodSymbol.Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generates class-level data source metadata
    /// </summary>
    public void GenerateClassDataSourceMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        GenerateDataSourceMetadataForLevel(writer, testInfo, DataSourceLevel.Class, "ClassDataSources");
    }

    /// <summary>
    /// Generates property data source metadata (for property injection)
    /// </summary>
    public void GeneratePropertyDataSourceMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var propertyDataSources = testInfo.TypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetAttributes().Any(a => a.AttributeClass?.Name == "DataSourceForAttribute"))
            .SelectMany(p => _extractor.ExtractDataSources(p, DataSourceLevel.Property, testInfo))
            .ToList();

        if (!propertyDataSources.Any())
        {
            writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
            return;
        }

        writer.AppendLine("PropertyDataSources = new PropertyDataSource[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var dataSource in propertyDataSources)
        {
            if (dataSource.PropertySymbol != null)
            {
                writer.AppendLine("new PropertyDataSource");
                writer.AppendLine("{");
                writer.Indent();

                writer.AppendLine($"PropertyName = \"{dataSource.PropertySymbol.Name}\",");
                writer.AppendLine($"PropertyType = typeof({dataSource.PropertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}),");
                writer.Append("DataSource = ");
                _codeGenerator.GenerateDataSourceInstance(writer, dataSource);

                writer.Unindent();
                writer.AppendLine("},");
            }
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    /// <summary>
    /// Unified method for generating data source metadata at any level
    /// </summary>
    private void GenerateDataSourceMetadataForLevel(CodeWriter writer, TestMethodMetadata testInfo,
        DataSourceLevel level, string propertyName)
    {
        var symbol = level == DataSourceLevel.Class ? testInfo.TypeSymbol : (ISymbol)testInfo.MethodSymbol;
        var dataSources = _extractor.ExtractDataSources(symbol, level, testInfo).ToList();

        // Property data sources are handled separately by GeneratePropertyDataSourceMetadata

        if (!dataSources.Any())
        {
            writer.AppendLine($"{propertyName} = Array.Empty<TestDataSource>(),");
            return;
        }

        writer.AppendLine($"{propertyName} = new TestDataSource[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var dataSource in dataSources)
        {
            _codeGenerator.GenerateDataSourceInstance(writer, dataSource);
        }

        writer.Unindent();
        writer.AppendLine("},");
    }
}
