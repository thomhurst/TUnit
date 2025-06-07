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

            using var sourceBuilder = new SourceCodeWriter();

            sourceBuilder.Write("#pragma warning disable");
            sourceBuilder.Write("using global::System.Linq;");
            sourceBuilder.Write("using global::System.Reflection;");
            sourceBuilder.Write("using global::TUnit.Core;");
            sourceBuilder.Write("using global::TUnit.Core.Extensions;");
            sourceBuilder.WriteLine();
            sourceBuilder.Write("namespace TUnit.SourceGenerated;");
            sourceBuilder.WriteLine();
            sourceBuilder.Write("[global::System.Diagnostics.StackTraceHidden]");
            sourceBuilder.Write("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
            sourceBuilder.Write(
                $"file partial class {className} : global::TUnit.Core.Interfaces.SourceGenerator.IDynamicTestSource");
            sourceBuilder.Write("{");

            sourceBuilder.Write("[global::System.Runtime.CompilerServices.ModuleInitializer]");
            sourceBuilder.Write("public static void Initialise()");
            sourceBuilder.Write("{");
            sourceBuilder.Write($"global::TUnit.Core.SourceRegistrar.RegisterDynamic(new {className}());");
            sourceBuilder.Write("}");

            sourceBuilder.Write(
                "public global::System.Collections.Generic.IReadOnlyList<DynamicTest> CollectDynamicTests(string sessionId)");
            sourceBuilder.Write("{");

            sourceBuilder.Write("try");
            sourceBuilder.Write("{");

            sourceBuilder.Write
            (
                $"""
                 var context = new global::TUnit.Core.DynamicTestBuilderContext(@"{dynamicTestSource.FilePath}", {dynamicTestSource.LineNumber});
                 """
            );

            var receiver = dynamicTestSource.Method.IsStatic
                ? dynamicTestSource.Class.GloballyQualified()
                : $"new {dynamicTestSource.Class.GloballyQualified()}()";

            sourceBuilder.Write($"{receiver}.{dynamicTestSource.Method.Name}(context);");

            sourceBuilder.Write("return context.Tests;");

            sourceBuilder.Write("}");
            sourceBuilder.Write("catch (global::System.Exception exception)");
            sourceBuilder.Write("{");
            FailedTestInitializationWriter.GenerateFailedTestCode(sourceBuilder, dynamicTestSource);
            sourceBuilder.Write("}");

            sourceBuilder.Write("}");

            sourceBuilder.Write("}");

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
