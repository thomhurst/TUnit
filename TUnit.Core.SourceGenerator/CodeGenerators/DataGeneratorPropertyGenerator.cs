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
                predicate: static (node, _) => node is ClassDeclarationSyntax
                    or RecordDeclarationSyntax
                    or StructDeclarationSyntax,
                transform: GetClassesWithDataSourceProperties)
            .SelectMany(static (s, _) => s)
            .WithComparer(SymbolEqualityComparer.Default);

        context.RegisterSourceOutput(classDataSourceClasses, GeneratePropertyInitializer!);
    }

    private static IEnumerable<INamedTypeSymbol> GetClassesWithDataSourceProperties(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var semanticModel = context.SemanticModel;

        if (semanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol classSymbol)
        {
            return [];
        }

        return CollectClass(classSymbol);
    }

    private static IEnumerable<INamedTypeSymbol> CollectClass(INamedTypeSymbol namedTypeSymbol)
    {
        if (namedTypeSymbol.IsGenericDefinition())
        {
            yield break;
        }

        var hashset = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        var nestedTypes = namedTypeSymbol
            .GetMembersIncludingBase()
            .OfType<IPropertySymbol>()
            .Select(x => x.Type)
            .OfType<INamedTypeSymbol>()
            .Where(x => IsOrHasEvent(x, hashset))
            .ToArray();

        foreach (var nestedType in nestedTypes)
        {
            foreach (var typeSymbol in CollectClass(nestedType))
            {
                yield return typeSymbol;
            }
        }

        if (nestedTypes.Length > 0)
        {
            yield return namedTypeSymbol;
        }
    }

    private static bool IsOrHasEvent(INamedTypeSymbol propertyType, HashSet<ITypeSymbol> visitedTypes)
    {
        if (propertyType.IsGenericDefinition() || !visitedTypes.Add(propertyType))
        {
            return false;
        }

        if (propertyType.SpecialType is not SpecialType.None
            and not SpecialType.System_IDisposable)
        {
            return false;
        }

        if (propertyType.AllInterfaces.Any(p =>
                p.GloballyQualified() == "global::TUnit.Core.Interfaces.IAsyncInitializer" ||
                p.GloballyQualified() == "global::System.IAsyncDisposable" ||
                p.GloballyQualified() == "global::System.IDisposable" ||
                p.GloballyQualified() == "global::TUnit.Core.Interfaces.IEventReceiver"))
        {
            return true;
        }

        return propertyType
            .GetMembersIncludingBase()
            .OfType<IPropertySymbol>()
            .Select(x => x.Type)
            .OfType<INamedTypeSymbol>()
            .Any(x => IsOrHasEvent(x, visitedTypes));
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

        var hashSet = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var propertySymbol in type
                     .GetMembersIncludingBase()
                     .OfType<IPropertySymbol>()
                     .Select(x => x.Type)
                     .OfType<INamedTypeSymbol>()
                     .Where(x => IsOrHasEvent(x, hashSet)))
        {
                RegisterProperty(propertySymbol, sourceBuilder);
        }
    }
}
