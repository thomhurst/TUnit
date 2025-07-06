using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Generators.Expansion;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Enhanced metadata generator that creates strongly-typed TestMetadata with compile-time expansion
/// </summary>
public sealed class TypedMetadataGenerator
{
    private readonly TestMetadataExpander _metadataExpander;

    public TypedMetadataGenerator(DataSourceGenerator dataSourceGenerator)
    {
        _metadataExpander = new TestMetadataExpander();
    }

    /// <summary>
    /// Generates expanded test metadata for all test methods with compile-time data expansion
    /// </summary>
    public void GenerateExpandedTestRegistrations(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        writer.AppendLine("var successCount = 0;");
        writer.AppendLine("var failedTests = new List<string>();");
        writer.AppendLine();

        foreach (var testInfo in testMethods)
        {
            writer.AppendLine("try");
            writer.AppendLine("{");
            writer.Indent();

            // Delegate to the metadata expander which handles all expansion strategies
            _metadataExpander.GenerateExpandedTestMetadata(writer, testInfo);

            writer.AppendLine("successCount++;");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("catch (Exception ex)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"var testName = \"{testInfo.TypeSymbol.ToDisplayString()}.{testInfo.MethodSymbol.Name}\";");
            writer.AppendLine("failedTests.Add($\"{testName}: {ex.Message}\");");
            writer.AppendLine("Console.Error.WriteLine($\"Failed to register test {testName}: {ex}\");");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine();
        }

        writer.AppendLine("if (failedTests.Count > 0)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("Console.Error.WriteLine($\"Failed to register {failedTests.Count} tests:\");");
        writer.AppendLine("foreach (var failure in failedTests)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("Console.Error.WriteLine($\"  - {failure}\");");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
    }
}