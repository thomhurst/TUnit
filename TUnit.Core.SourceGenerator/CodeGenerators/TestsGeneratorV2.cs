using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

/// <summary>
/// Enhanced TestsGenerator that can emit either legacy execution logic or new TestMetadata
/// based on compilation configuration.
/// </summary>
// [Generator] // Disabled - using TestMetadataGenerator instead
public class TestsGeneratorV2 : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Check if UseTestBuilder is enabled via compilation options
        var useTestBuilderProvider = context.CompilationProvider
            .Select((compilation, _) => IsTestBuilderEnabled(compilation));

        var standardTests = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.TestAttribute",
                predicate: static (_, _) => true,
                transform: static (ctx, _) => GetSemanticTargetForTestMethodGeneration(ctx))
            .Where(static m => m is not null);

        var inheritedTests = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.InheritsTestsAttribute",
                predicate: static (_, _) => true,
                transform: static (ctx, _) => GetSemanticTargetForInheritedTestsGeneration(ctx))
            .Where(static m => m is not null);

        // Combine test collections with the configuration flag
        var standardTestsWithConfig = standardTests.Combine(useTestBuilderProvider);
        var inheritedTestsWithConfig = inheritedTests.Combine(useTestBuilderProvider);

        context.RegisterSourceOutput(standardTestsWithConfig, 
            (sourceContext, data) => GenerateTests(sourceContext, data.Left!, data.Right));
        
        context.RegisterSourceOutput(inheritedTestsWithConfig, 
            (sourceContext, data) => GenerateTests(sourceContext, data.Left!, data.Right, "Inherited_"));
    }

    private static bool IsTestBuilderEnabled(Compilation compilation)
    {
        // Check for MSBuild property passed via global options
        if (compilation.Options is CSharpCompilationOptions csharpOptions)
        {
            // Check for preprocessor symbol
            if (compilation.SyntaxTrees.Any(tree => 
                tree.Options.PreprocessorSymbolNames.Contains("TUNIT_USE_TEST_BUILDER")))
            {
                return true;
            }
        }

        // Default to false for backward compatibility
        return false;
    }

    static TestCollectionDataModel? GetSemanticTargetForTestMethodGeneration(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        if (methodSymbol.ContainingType.IsAbstract)
        {
            return null;
        }

        if (methodSymbol.IsStatic)
        {
            return null;
        }

        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        if (methodSymbol.ContainingType.IsGenericDefinition())
        {
            return null;
        }

        return new TestCollectionDataModel(methodSymbol.ParseTestDatas(context, methodSymbol.ContainingType));
    }

    static TestCollectionDataModel? GetSemanticTargetForInheritedTestsGeneration(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return null;
        }

        if (namedTypeSymbol.IsAbstract)
        {
            return null;
        }

        if (namedTypeSymbol.IsStatic)
        {
            return null;
        }

        if (namedTypeSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        if (namedTypeSymbol.IsGenericDefinition())
        {
            return null;
        }

        return new TestCollectionDataModel(
            namedTypeSymbol.GetBaseTypes()
                .SelectMany(x => x.GetMembers())
                .OfType<IMethodSymbol>()
                .Where(x => !x.IsAbstract)
                .Where(x => x.MethodKind != MethodKind.Constructor)
                .Where(x => x.IsTest())
                .SelectMany(x => x.ParseTestDatas(context, namedTypeSymbol))
        );
    }

    private void GenerateTests(SourceProductionContext context, TestCollectionDataModel testCollection, bool useTestBuilder, string? prefix = null)
    {
        if (useTestBuilder)
        {
            GenerateTestMetadata(context, testCollection, prefix);
        }
        else
        {
            GenerateLegacyTests(context, testCollection, prefix);
        }
    }

    private void GenerateTestMetadata(SourceProductionContext context, TestCollectionDataModel testCollection, string? prefix)
    {
        try
        {
            var allTestSources = testCollection.TestSourceDataModels.ToList();
            if (!allTestSources.Any())
                return;

            using var sourceBuilder = new SourceCodeWriter();

            // Generate imports
            sourceBuilder.Write("using global::System;");
            sourceBuilder.Write("using global::System.Collections.Generic;");
            sourceBuilder.Write("using global::System.Linq;");
            sourceBuilder.Write("using global::System.Reflection;");
            sourceBuilder.Write("using global::TUnit.Core;");
            sourceBuilder.Write("using global::TUnit.Core.DataSources;");
            sourceBuilder.Write("using global::TUnit.Core.Configuration;");
            sourceBuilder.Write("using global::TUnit.Core.SourceGenerator;");
            sourceBuilder.WriteLine();

            sourceBuilder.Write("namespace TUnit.SourceGenerated;");
            sourceBuilder.WriteLine();

            var className = $"{prefix}TestMetadataRegistry_{Guid.NewGuid():N}";

            sourceBuilder.Write("[global::System.Diagnostics.StackTraceHidden]");
            sourceBuilder.Write("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
            sourceBuilder.Write($"[System.CodeDom.Compiler.GeneratedCode(\"TUnit\", \"{typeof(TestsGeneratorV2).Assembly.GetName().Version}\")]");
            sourceBuilder.Write($"file static class {className}");
            sourceBuilder.Write("{");

            // Module initializer
            sourceBuilder.Write("[global::System.Runtime.CompilerServices.ModuleInitializer]");
            sourceBuilder.Write("public static void Initialize()");
            sourceBuilder.Write("{");
            sourceBuilder.Write("if (global::TUnit.Core.Configuration.TUnitConfiguration.UseTestBuilder)");
            sourceBuilder.Write("{");
            sourceBuilder.Write("var testMetadata = new global::System.Collections.Generic.List<global::TUnit.Core.TestMetadata>();");
            
            // Generate metadata for each test
            int testIndex = 0;
            foreach (var testSource in allTestSources)
            {
                GenerateSingleTestMetadata(sourceBuilder, testSource, testIndex++);
            }

            sourceBuilder.Write("global::TUnit.Core.SourceGenerator.TestSourceRegistrar.RegisterMetadata(testMetadata);");
            sourceBuilder.Write("}");
            sourceBuilder.Write("}");

            // Generate helper methods for each test
            testIndex = 0;
            foreach (var testSource in allTestSources)
            {
                GenerateTestMetadataHelpers(sourceBuilder, testSource, testIndex++);
            }

            sourceBuilder.Write("}");

            context.AddSource($"{className}.g.cs", sourceBuilder.ToString());
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "TUG0001",
                    "Test generation failed",
                    $"Failed to generate tests: {ex.Message}",
                    "TUnit.Generator",
                    DiagnosticSeverity.Error,
                    true),
                Location.None));
        }
    }

    private void GenerateSingleTestMetadata(SourceCodeWriter sourceBuilder, TestSourceDataModel testSource, int index)
    {
        sourceBuilder.Write($"testMetadata.Add(CreateTestMetadata_{index}());");
    }

    private void GenerateTestMetadataHelpers(SourceCodeWriter sourceBuilder, TestSourceDataModel testSource, int index)
    {
        sourceBuilder.WriteLine();
        sourceBuilder.Write($"private static global::TUnit.Core.TestMetadata CreateTestMetadata_{index}()");
        sourceBuilder.Write("{");
        
        // Build test ID template
        var testIdTemplate = $"{testSource.FullyQualifiedClassIncludingParentClasses}.{testSource.MethodName}_{{TestIndex}}";
        if (testSource.RepeatLimit > 0)
        {
            testIdTemplate += "_Repeat_{RepeatIndex}";
        }

        sourceBuilder.Write("return new global::TUnit.Core.TestMetadata");
        sourceBuilder.Write("{");
        sourceBuilder.Write($"TestIdTemplate = @\"{testIdTemplate}\",");
        sourceBuilder.Write($"TestClassType = typeof({testSource.FullyQualifiedClassIncludingParentClasses}),");
        sourceBuilder.Write($"TestMethod = typeof({testSource.FullyQualifiedClassIncludingParentClasses}).GetMethod(\"{testSource.MethodName}\", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance),");
        sourceBuilder.Write($"MethodMetadata = CreateMethodMetadata_{index}(),");
        sourceBuilder.Write($"TestFilePath = @\"{testSource.TestLocation.FilePath}\",");
        sourceBuilder.Write($"TestLineNumber = {testSource.TestLocation.MethodSpan.StartLinePosition.Line + 1},");
        
        // Generate class factory
        GenerateClassFactory(sourceBuilder, testSource);
        
        // Generate data sources
        GenerateDataSourceProviders(sourceBuilder, testSource);
        
        // Display name template
        sourceBuilder.Write($"DisplayNameTemplate = @\"{BuildDisplayNameTemplate(testSource)}\",");
        sourceBuilder.Write($"RepeatCount = {testSource.RepeatLimit + 1},");
        sourceBuilder.Write($"IsAsync = {(testSource.IsAsync ? "true" : "false")},");
        sourceBuilder.Write($"IsSkipped = {(testSource.IsSkipped ? "true" : "false")},");
        
        if (testSource.IsSkipped && !string.IsNullOrEmpty(testSource.SkipReason))
        {
            sourceBuilder.Write($"SkipReason = @\"{testSource.SkipReason}\",");
        }
        
        sourceBuilder.Write($"Attributes = new global::System.Attribute[] {{ }},"); // TODO: Collect actual attributes
        
        if (testSource.TimeoutMs.HasValue)
        {
            sourceBuilder.Write($"Timeout = global::System.TimeSpan.FromMilliseconds({testSource.TimeoutMs.Value})");
        }
        else
        {
            sourceBuilder.Write("Timeout = null");
        }
        
        sourceBuilder.Write("};");
        sourceBuilder.Write("}");

        // Generate MethodMetadata helper
        GenerateMethodMetadataHelper(sourceBuilder, testSource, index);
    }

    private void GenerateClassFactory(SourceCodeWriter sourceBuilder, TestSourceDataModel testSource)
    {
        sourceBuilder.Write("TestClassFactory = (args) =>");
        sourceBuilder.Write("{");
        
        if (testSource.IsStaticClass)
        {
            sourceBuilder.Write("return null; // Static class");
        }
        else
        {
            // Use the existing NewClassWriter logic but simplified
            sourceBuilder.Write($"return new {testSource.FullyQualifiedClassIncludingParentClasses}(");
            
            // TODO: Handle constructor arguments properly
            if (testSource.ClassArguments.Count > 0)
            {
                sourceBuilder.Write("/* Constructor args from args parameter */");
            }
            
            sourceBuilder.Write(");");
        }
        
        sourceBuilder.Write("},");
    }

    private void GenerateDataSourceProviders(SourceCodeWriter sourceBuilder, TestSourceDataModel testSource)
    {
        // Class data sources
        sourceBuilder.Write("ClassDataSources = new global::TUnit.Core.IDataSourceProvider[]");
        sourceBuilder.Write("{");
        // TODO: Extract class data sources from testSource.ClassArguments
        sourceBuilder.Write("},");
        
        // Method data sources
        sourceBuilder.Write("MethodDataSources = new global::TUnit.Core.IDataSourceProvider[]");
        sourceBuilder.Write("{");
        
        foreach (var methodArg in testSource.MethodArguments)
        {
            if (methodArg is InlineArgumentsContainer inline)
            {
                sourceBuilder.Write($"new global::TUnit.Core.DataSources.InlineDataSourceProvider({string.Join(", ", inline.ArgumentValues.Select(FormatValue))}),");
            }
            else if (methodArg is MethodDataSourceAttributeContainer methodDataSource)
            {
                // TODO: Generate MethodDataSourceProvider
                sourceBuilder.Write("// TODO: MethodDataSourceProvider,");
            }
            // Handle other data source types...
        }
        
        sourceBuilder.Write("},");
        
        // Property data sources
        sourceBuilder.Write("PropertyDataSources = new global::System.Collections.Generic.Dictionary<global::System.Reflection.PropertyInfo, global::TUnit.Core.IDataSourceProvider>");
        sourceBuilder.Write("{");
        // TODO: Extract property data sources
        sourceBuilder.Write("},");
    }

    private void GenerateMethodMetadataHelper(SourceCodeWriter sourceBuilder, TestSourceDataModel testSource, int index)
    {
        sourceBuilder.WriteLine();
        sourceBuilder.Write($"private static global::TUnit.Core.MethodMetadata CreateMethodMetadata_{index}()");
        sourceBuilder.Write("{");
        sourceBuilder.Write("// TODO: Generate proper MethodMetadata");
        sourceBuilder.Write("return new global::TUnit.Core.MethodMetadata");
        sourceBuilder.Write("{");
        sourceBuilder.Write($"Name = \"{testSource.MethodName}\",");
        sourceBuilder.Write($"Type = typeof({testSource.FullyQualifiedClassIncludingParentClasses}),");
        sourceBuilder.Write("Parameters = global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>(),");
        sourceBuilder.Write("GenericTypeCount = 0,");
        sourceBuilder.Write("Class = null, // TODO: Generate ClassMetadata");
        sourceBuilder.Write($"ReturnType = typeof({(testSource.IsAsync ? "global::System.Threading.Tasks.Task" : "void")}),");
        sourceBuilder.Write("Attributes = global::System.Array.Empty<global::TUnit.Core.AttributeMetadata>()");
        sourceBuilder.Write("};");
        sourceBuilder.Write("}");
    }

    private void GenerateLegacyTests(SourceProductionContext context, TestCollectionDataModel testCollection, string? prefix)
    {
        // This would contain the existing test generation logic
        // For now, just delegate to the original implementation
        // In a real implementation, this would contain the full legacy generation code
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "TUG0002",
                "Legacy generation not implemented",
                "Legacy test generation is not implemented in this example",
                "TUnit.Generator",
                DiagnosticSeverity.Warning,
                true),
            Location.None));
    }

    private string BuildDisplayNameTemplate(TestSourceDataModel testSource)
    {
        if (testSource.MethodArguments.Count == 0)
            return testSource.MethodName;
        
        // Build template with placeholders
        var paramCount = testSource.MethodParameters.Length;
        var placeholders = string.Join(", ", Enumerable.Range(0, paramCount).Select(i => $"{{{i}}}"));
        return $"{testSource.MethodName}({placeholders})";
    }

    private string FormatValue(object? value)
    {
        if (value == null)
            return "null";
        if (value is string s)
            return $"\"{s.Replace("\"", "\\\"")}\"";
        if (value is bool b)
            return b.ToString().ToLower();
        if (value is char c)
            return $"'{c}'";
        // Add more type handling as needed
        return value.ToString() ?? "null";
    }
}