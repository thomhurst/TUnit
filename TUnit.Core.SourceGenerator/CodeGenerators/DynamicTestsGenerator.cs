using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

[Generator]
public class DynamicTestsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var standardTests = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.DynamicTestBuilderAttribute",
                predicate: static (_, _) => true,
                transform: static (ctx, _) => GetSemanticTargetForTestMethodGeneration(ctx))
            .Where(static m => m is not null);

        context.RegisterSourceOutput(standardTests, (sourceContext, data) => GenerateTests(sourceContext, data!));
    }

    static DynamicTestSourceDataModel? GetSemanticTargetForTestMethodGeneration(GeneratorAttributeSyntaxContext context)
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

        return methodSymbol.ParseDynamicTestBuilders();
    }

    private void GenerateTests(SourceProductionContext context, DynamicTestSourceDataModel dynamicTestSource, string? prefix = null)
    {
        try
        {
            var className = dynamicTestSource.Class.Name;

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
            sourceBuilder.AppendLine($"[System.CodeDom.Compiler.GeneratedCode(\"TUnit\", \"{typeof(DynamicTestsGenerator).Assembly.GetName().Version}\")]");
            using (sourceBuilder.BeginBlock($"file partial class {className} : global::TUnit.Core.Interfaces.SourceGenerator.IDynamicTestSource"))
            {
                sourceBuilder.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
                using (sourceBuilder.BeginBlock("public static void Initialise()"))
                {
                    sourceBuilder.AppendLine($"global::TUnit.Core.SourceRegistrar.RegisterDynamic(new {className}());");
                }

                sourceBuilder.EnsureNewLine();
                using (sourceBuilder.BeginBlock("public global::System.Collections.Generic.IReadOnlyList<DynamicTest> CollectDynamicTests(string sessionId)"))
                {
                    using (sourceBuilder.BeginBlock("try"))
                    {
                        sourceBuilder.AppendLine(
                            $"""
                             var context = new global::TUnit.Core.DynamicTestBuilderContext(@"{dynamicTestSource.FilePath}", {dynamicTestSource.LineNumber});
                             """);

                        var receiver = dynamicTestSource.Method.IsStatic
                            ? dynamicTestSource.Class.GloballyQualified()
                            : $"new {dynamicTestSource.Class.GloballyQualified()}()";

                        sourceBuilder.AppendLine($"{receiver}.{dynamicTestSource.Method.Name}(context);");
                        sourceBuilder.AppendLine("return context.Tests;");
                    }
                    using (sourceBuilder.BeginBlock("catch (global::System.Exception exception)"))
                    {
                        FailedTestInitializationWriter.GenerateFailedTestCode(sourceBuilder, dynamicTestSource);
                    }
                }
            }

            context.AddSource($"Dynamic-{className}-{Guid.NewGuid():N}.Generated.cs", sourceBuilder.ToString());
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
}
