using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Builders;

/// <summary>
/// Interface for building test definitions
/// </summary>
internal interface ITestDefinitionBuilder
{
    /// <summary>
    /// Determines if this builder can handle the given context
    /// </summary>
    bool CanBuild(TestMetadataGenerationContext context);

    /// <summary>
    /// Builds test definitions and writes them to the code writer
    /// </summary>
    void BuildTestDefinitions(CodeWriter writer, TestMetadataGenerationContext context);
}
