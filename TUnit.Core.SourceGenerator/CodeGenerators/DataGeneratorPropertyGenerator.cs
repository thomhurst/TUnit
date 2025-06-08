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
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: GetClassesWithDataSourceProperties)
            .SelectMany(static (symbols, _) => symbols)
            .WithComparer(SymbolEqualityComparer.Default);

        context.RegisterSourceOutput(classDataSourceClasses, GeneratePropertyInitializer);
    }

    private static IEnumerable<INamedTypeSymbol> GetClassesWithDataSourceProperties(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return [];
        }

        var semanticModel = context.SemanticModel;

        if(semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
        {
            return [];
        }

        return classSymbol.
            GetMembersIncludingBase()
            .OfType<IPropertySymbol>()
            .Where(x => HasDataGeneratorProperties(x) || IsAsyncInitializer(x.Type))
            .Select(x => x.Type)
            .OfType<INamedTypeSymbol>();
    }

    private static bool IsAsyncInitializer(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol namedTypeSymbol)
        {
            return false;
        }

        return namedTypeSymbol.AllInterfaces.Any(x => x.GloballyQualified() == "global::TUnit.Core.Interfaces.IAsyncInitializer");
    }

    private static bool HasDataGeneratorProperties(IPropertySymbol property)
    {
        return property.GetAttributes().Any(attr => IsDataSourceGeneratorAttribute(attr.AttributeClass, out _));
    }

    private static bool IsDataSourceGeneratorAttribute(INamedTypeSymbol? attributeClass, out INamedTypeSymbol? attributeBaseType)
    {
        if (attributeClass == null)
        {
            attributeBaseType = null;
            return false;
        }

        foreach (var type in attributeClass.GetSelfAndBaseTypes())
        {
            if(type.Interfaces.Any(i =>
                i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::TUnit.Core.IDataSourceGeneratorAttribute"))
            {
                attributeBaseType = type;
                return true;
            }
        }

        attributeBaseType = null;
        return false;
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

        foreach (var propertySymbol in type.GetMembersIncludingBase().OfType<IPropertySymbol>())
        {
            if (!propertySymbol.GetAttributes().Any(x => IsDataSourceGeneratorAttribute(x.AttributeClass, out _)
                    || IsAsyncInitializer(x.AttributeClass)))
            {
                continue;
            }

            if (propertySymbol.Type is INamedTypeSymbol namedTypePropertySymbol)
            {
                RegisterProperty(namedTypePropertySymbol, sourceBuilder);
            }
        }
    }
}
