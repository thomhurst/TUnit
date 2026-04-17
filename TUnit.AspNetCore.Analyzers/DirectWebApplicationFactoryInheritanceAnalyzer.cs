using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.AspNetCore.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DirectWebApplicationFactoryInheritanceAnalyzer : ConcurrentDiagnosticAnalyzer
{
    private const string WebApplicationFactoryMetadataName = "Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory`1";
    private const string TestWebApplicationFactoryMetadataName = "TUnit.AspNetCore.TestWebApplicationFactory`1";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.DirectWebApplicationFactoryInheritance);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var webApplicationFactory = compilationContext.Compilation
                .GetTypeByMetadataName(WebApplicationFactoryMetadataName);

            if (webApplicationFactory is null)
            {
                return;
            }

            var testWebApplicationFactory = compilationContext.Compilation
                .GetTypeByMetadataName(TestWebApplicationFactoryMetadataName);

            if (testWebApplicationFactory is null)
            {
                return;
            }

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, webApplicationFactory, testWebApplicationFactory),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        INamedTypeSymbol webApplicationFactory,
        INamedTypeSymbol testWebApplicationFactory)
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class, BaseType: { } baseType } type)
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, webApplicationFactory))
        {
            return;
        }

        if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, testWebApplicationFactory))
        {
            return;
        }

        var location = GetBaseTypeLocation(type) ?? type.Locations.FirstOrDefault();
        if (location is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rules.DirectWebApplicationFactoryInheritance,
            location,
            type.Name));
    }

    private static Location? GetBaseTypeLocation(INamedTypeSymbol type)
    {
        foreach (var syntaxRef in type.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is not TypeDeclarationSyntax typeDeclaration)
            {
                continue;
            }

            var baseList = typeDeclaration.BaseList;
            if (baseList is null || baseList.Types.Count == 0)
            {
                continue;
            }

            return baseList.Types[0].GetLocation();
        }

        return null;
    }
}
