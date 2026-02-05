using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Models.Extracted;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

[Generator]
public class DynamicTestsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enabledProvider = context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.EnableTUnitSourceGeneration", out var value);
                return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
            });

        var standardTests = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.DynamicTestBuilderAttribute",
                predicate: static (_, _) => true,
                transform: static (ctx, _) => ExtractDynamicTestModel(ctx))
            .Where(static m => m is not null)
            .Combine(enabledProvider);

        context.RegisterSourceOutput(standardTests, (sourceContext, data) =>
        {
            var (testData, isEnabled) = data;
            if (!isEnabled)
            {
                return;
            }

            GenerateTests(sourceContext, testData!);
        });
    }

    /// <summary>
    /// Extracts all needed data as primitives in the transform step.
    /// This enables proper incremental caching.
    /// </summary>
    private static DynamicTestModel? ExtractDynamicTestModel(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        if (methodSymbol.IsAbstract)
        {
            return null;
        }

        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        var containingType = methodSymbol.ContainingType;
        var testAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "DynamicTestBuilderAttribute");

        var filePath = testAttribute?.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString() ?? string.Empty;
        var lineNumber = testAttribute?.ConstructorArguments.ElementAtOrDefault(1).Value as int? ?? 0;

        // Extract ALL data as primitives - no symbols escape this method
        return new DynamicTestModel
        {
            FullyQualifiedTypeName = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MinimalTypeName = containingType.Name,
            Namespace = containingType.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            MethodName = methodSymbol.Name,
            IsStatic = methodSymbol.IsStatic,
            IsAsync = methodSymbol.IsAsync,
            ReturnType = methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            FilePath = filePath,
            LineNumber = lineNumber
        };
    }

    private static void GenerateTests(SourceProductionContext context, DynamicTestModel model)
    {
        try
        {
            var className = model.MinimalTypeName;

            using var sourceBuilder = new CodeWriter();

            sourceBuilder.AppendLine("using global::System.Linq;");
            sourceBuilder.AppendLine("using global::System.Reflection;");
            sourceBuilder.AppendLine("using global::TUnit.Core;");
            sourceBuilder.AppendLine("using global::TUnit.Core.Extensions;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace TUnit.SourceGenerated;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("[global::System.Diagnostics.StackTraceHidden]");
            sourceBuilder.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
            sourceBuilder.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCode(\"TUnit\", \"{typeof(DynamicTestsGenerator).Assembly.GetName().Version}\")]");
            using (sourceBuilder.BeginBlock($"file partial class {className} : global::TUnit.Core.IDynamicTestSource"))
            {
                sourceBuilder.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
                using (sourceBuilder.BeginBlock("public static void Initialise()"))
                {
                    sourceBuilder.AppendLine($"global::TUnit.Core.SourceRegistrar.RegisterDynamic(new {className}());");
                }

                sourceBuilder.EnsureNewLine();
                using (sourceBuilder.BeginBlock("public global::System.Collections.Generic.IReadOnlyList<global::TUnit.Core.AbstractDynamicTest> CollectDynamicTests(string sessionId)"))
                {
                    using (sourceBuilder.BeginBlock("try"))
                    {
                        sourceBuilder.AppendLine(
                            $"""
                             var context = new global::TUnit.Core.DynamicTestBuilderContext(@"{model.FilePath}", {model.LineNumber});
                             """);

                        var receiver = model.IsStatic
                            ? model.FullyQualifiedTypeName
                            : $"new {model.FullyQualifiedTypeName}()";

                        sourceBuilder.AppendLine($"{receiver}.{model.MethodName}(context);");
                        sourceBuilder.AppendLine("return context.Tests;");
                    }
                    using (sourceBuilder.BeginBlock("catch (global::System.Exception exception)"))
                    {
                        GenerateFailedTestCode(sourceBuilder, model);
                    }
                }
            }

            // Deterministic filename - no GUID needed since file keyword prevents collisions
            // and each method is unique within its type
            context.AddSource($"Dynamic_{className}_{model.MethodName}.g.cs", sourceBuilder.ToString());
        }
        catch (Exception ex)
        {
            var descriptor = new DiagnosticDescriptor(id: "TUnit0000",
                title: "Error Generating Source",
                messageFormat: "{0}",
                category: "SourceGenerator",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            context.ReportDiagnostic(Diagnostic.Create(descriptor, null, ex.ToString()));
        }
    }

    private static void GenerateFailedTestCode(CodeWriter sourceBuilder, DynamicTestModel model)
    {
        sourceBuilder.AppendLine(
            $$"""
              return new global::System.Collections.Generic.List<global::TUnit.Core.AbstractDynamicTest>
                          {
                              new global::TUnit.Core.FailedDynamicTest<{{model.FullyQualifiedTypeName}}>
                              {
                                  MethodName = "{{model.MethodName}}",
                                  TestFilePath = @"{{model.FilePath}}",
                                  TestLineNumber = {{model.LineNumber}},
                                  Exception = exception
                              }
                          };
              """);
    }
}
