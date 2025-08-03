using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Analyzers;

/// <summary>
/// Interface for analyzing test methods from source code.
/// </summary>
public interface ITestAnalyzer
{
    /// <summary>
    /// Analyzes a method with a Test attribute and returns its model.
    /// </summary>
    TestMethodModel? AnalyzeMethod(GeneratorAttributeSyntaxContext context);
}

/// <summary>
/// Interface for analyzing data sources on test methods.
/// </summary>
public interface IDataSourceAnalyzer
{
    /// <summary>
    /// Analyzes data source attributes on a method and returns bindings.
    /// </summary>
    DataSourceBinding? AnalyzeMethodDataSource(IMethodSymbol method);

    /// <summary>
    /// Analyzes data source attributes on a class and returns bindings.
    /// </summary>
    DataSourceBinding? AnalyzeClassDataSource(INamedTypeSymbol type);

    /// <summary>
    /// Analyzes data source attributes on properties and returns bindings.
    /// </summary>
    IReadOnlyDictionary<string, DataSourceBinding>? AnalyzePropertyDataSources(INamedTypeSymbol type);
}
