using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class XUnitAttributesAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.XunitAttributes);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method, SymbolKind.NamedType);
        // context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }
    
    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        foreach (var attributeData in context.Symbol.GetAttributes())
        {
            var @namespace = attributeData.AttributeClass?.ContainingNamespace?.Name;
            
            if(@namespace == "Xunit")
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.XunitAttributes, attributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation())
                );
            }
        }
    }

    private void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
    {
        var attributeSyntax = (AttributeSyntax)context.Node;
        
        var attributeListSyntax = attributeSyntax.Parent as AttributeListSyntax;

        if (attributeListSyntax?.Target?.Identifier.ValueText is not "assembly")
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;

        if (symbol is not IMethodSymbol)
        {
            return;
        }
        
        if (symbol.ContainingNamespace.Name.StartsWith("Xunit"))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.XunitAttributes, attributeSyntax.GetLocation())
            );
        }
    }
}