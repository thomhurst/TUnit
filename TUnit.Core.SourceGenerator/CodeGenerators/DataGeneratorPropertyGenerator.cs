using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

[Generator]
public class DataGeneratorPropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDataSourceClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax,
                transform: GetClassesWithDataSourceProperties)
            .Where(static s => s != null)
            .WithComparer(SymbolEqualityComparer.Default);

        context.RegisterSourceOutput(classDataSourceClasses, GeneratePropertyInitializer!);
    }

    private static INamedTypeSymbol? GetClassesWithDataSourceProperties(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var semanticModel = context.SemanticModel;

        if (semanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        return CollectClass(classSymbol);
    }

    private static INamedTypeSymbol? CollectClass(INamedTypeSymbol namedTypeSymbol)
    {
        if (namedTypeSymbol.IsGenericDefinition())
        {
            return null;
        }

        return namedTypeSymbol
            .GetMembersIncludingBase()
            .OfType<IPropertySymbol>()
            .Select(x => x.Type)
            .OfType<INamedTypeSymbol>()
            .Where(IsEvent).Any() ? namedTypeSymbol : null;
    }

    private static bool IsEvent(INamedTypeSymbol propertyType)
    {
        if (propertyType.IsGenericDefinition())
        {
            return false;
        }

        return propertyType.AllInterfaces.Any(p =>
            p.GloballyQualified() == "global::TUnit.Core.Interfaces.IAsyncInitializer" ||
            p.GloballyQualified() == "global::System.IAsyncDisposable" ||
            p.GloballyQualified() == "global::TUnit.Core.Interfaces.IEventReceiver");
    }

    private void GeneratePropertyInitializer(SourceProductionContext context, INamedTypeSymbol type)
    {
        try
        {
            using var sourceBuilder = new SourceCodeWriter();

            sourceBuilder.Write("using global::System;");
            sourceBuilder.Write("using global::System.Collections.Generic;");
            sourceBuilder.Write("using global::TUnit.Core;");
            sourceBuilder.WriteLine();

            var initializerClassName = $"{type.Name}PropertyInitializer_{Guid.NewGuid():N}";

            sourceBuilder.Write("namespace TUnit.SourceGenerated;");
            sourceBuilder.WriteLine();
            sourceBuilder.Write("[global::System.Diagnostics.StackTraceHidden]");
            sourceBuilder.Write("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
            sourceBuilder.Write($"[System.CodeDom.Compiler.GeneratedCode(\"TUnit\", \"{typeof(TestsGenerator).Assembly.GetName().Version}\")]");
            sourceBuilder.Write($"file static class {initializerClassName}");
            sourceBuilder.Write("{");

            sourceBuilder.Write("[global::System.Runtime.CompilerServices.ModuleInitializer]");
            sourceBuilder.Write("public static void InitializeProperties()");
            sourceBuilder.Write("{");

            RegisterProperty(type, sourceBuilder);

            sourceBuilder.Write("}");
            sourceBuilder.Write("}");

            context.AddSource($"PropertyInitializer_{initializerClassName}.g.cs", sourceBuilder.ToString());
        }
        catch (Exception ex)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "TUNIT_PROP_001",
                title: "Error generating property initializer",
                messageFormat: "Failed to generate property initializer: {0}",
                category: "SourceGenerator",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            context.ReportDiagnostic(Diagnostic.Create(descriptor, null, ex.ToString()));

            throw;
        }
    }

    private static void RegisterProperty(INamedTypeSymbol type, SourceCodeWriter sourceBuilder)
    {
        sourceBuilder.Write($"global::TUnit.Core.SourceRegistrar.RegisterProperty<{type.GloballyQualified()}>();");

        foreach (var propertySymbol in type
                     .GetMembersIncludingBase()
                     .OfType<IPropertySymbol>()
                     .Select(x => x.Type)
                     .OfType<INamedTypeSymbol>()
                     .Where(IsEvent))
        {
                RegisterProperty(propertySymbol, sourceBuilder);
        }
    }
}
