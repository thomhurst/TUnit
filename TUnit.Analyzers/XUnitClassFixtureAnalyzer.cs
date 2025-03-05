using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class XUnitClassFixtureAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.XunitClassFixtures);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SimpleBaseType);
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not SimpleBaseTypeSyntax simpleBaseTypeSyntax)
        {
            return;
        }

        var interfaceSymbol = context.SemanticModel.GetSymbolInfo(simpleBaseTypeSyntax.Type).Symbol;
        
        if (interfaceSymbol is INamedTypeSymbol { TypeKind: TypeKind.Interface, IsGenericType: true }
            && SymbolEqualityComparer.Default.Equals(interfaceSymbol.OriginalDefinition,
                context.Compilation.GetTypeByMetadataName("Xunit.IClassFixture`1")))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.XunitClassFixtures, simpleBaseTypeSyntax.GetLocation())
            );
        }
    }
}