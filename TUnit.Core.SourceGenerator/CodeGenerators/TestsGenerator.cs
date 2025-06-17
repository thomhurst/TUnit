using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

[Generator]
public class TestsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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

        context.RegisterSourceOutput(standardTests, (sourceContext, data) => GenerateTests(sourceContext, data!));
        context.RegisterSourceOutput(inheritedTests, (sourceContext, data) => GenerateTests(sourceContext, data!, "Inherited_"));
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

    private void GenerateTests(SourceProductionContext context, TestCollectionDataModel testCollection, string? prefix = null)
    {
        try
        {
            foreach (var classGrouping in testCollection
                         .TestSourceDataModels
                         .GroupBy(x => $"{prefix}{x.ClassNameToGenerate}"))
            {
                var className = classGrouping.Key;
                var count = classGrouping.Count();

                using var sourceBuilder = new SourceCodeWriter();

                sourceBuilder.Write("using global::System.Linq;");
                sourceBuilder.Write("using global::System.Reflection;");
                sourceBuilder.Write("using global::TUnit.Core;");
                sourceBuilder.Write("using global::TUnit.Core.Extensions;");
                sourceBuilder.WriteLine();
                sourceBuilder.Write("namespace TUnit.SourceGenerated;");
                sourceBuilder.WriteLine();
                sourceBuilder.Write("[global::System.Diagnostics.StackTraceHidden]");
                sourceBuilder.Write("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
                sourceBuilder.Write($"[System.CodeDom.Compiler.GeneratedCode(\"TUnit\", \"{typeof(TestsGenerator).Assembly.GetName().Version}\")]");
                sourceBuilder.Write(
                    $"file partial class {className} : global::TUnit.Core.Interfaces.SourceGenerator.ITestSource");
                sourceBuilder.Write("{");

                sourceBuilder.Write("[global::System.Runtime.CompilerServices.ModuleInitializer]");
                sourceBuilder.Write("public static void Initialise()");
                sourceBuilder.Write("{");
                sourceBuilder.Write($"global::TUnit.Core.SourceRegistrar.Register(new {className}());");
                sourceBuilder.Write("}");
                sourceBuilder.WriteLine();

                sourceBuilder.Write(
                    "public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IReadOnlyList<TestConstructionData>> CollectTestsAsync(string sessionId)");
                sourceBuilder.Write("{");
                if (count == 1)
                {
                    sourceBuilder.Write("return await Tests0(sessionId);");
                }
                else
                {
                    sourceBuilder.Write("var results = new global::System.Collections.Generic.List<TestConstructionData>();");
                    for (var i = 0; i < count; i++)
                    {
                        sourceBuilder.Write($"results.AddRange(await Tests{i}(sessionId));");
                    }
                    sourceBuilder.Write("return results;");
                }

                sourceBuilder.Write("}");
                sourceBuilder.WriteLine();

                var index = 0;
                foreach (var model in classGrouping)
                {
                    sourceBuilder.Write(
                        $"private async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<TestConstructionData>> Tests{index++}(string sessionId)");
                    sourceBuilder.Write("{");
                    sourceBuilder.Write(
                        "global::System.Collections.Generic.List<TestConstructionData> nodes = [];");
                    sourceBuilder.Write($"var {VariableNames.ClassDataIndex} = 0;");
                    sourceBuilder.Write($"var {VariableNames.TestMethodDataIndex} = 0;");

                    sourceBuilder.Write("try");
                    sourceBuilder.Write("{");
                    GenericTestInvocationWriter.GenerateTestInvocationCode(context, sourceBuilder, model);
                    sourceBuilder.Write("}");
                    sourceBuilder.Write("catch (global::System.Exception exception)");
                    sourceBuilder.Write("{");
                    FailedTestInitializationWriter.GenerateFailedTestCode(sourceBuilder, model);
                    sourceBuilder.Write("}");

                    sourceBuilder.Write("return nodes;");
                    sourceBuilder.Write("}");
                    sourceBuilder.WriteLine();
                }

                sourceBuilder.Write("}");

                context.AddSource($"{className}-{Guid.NewGuid():N}.Generated.cs", sourceBuilder.ToString());
            }
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

            throw;
        }
    }
}
