using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class XUnitAttributesAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.XunitAttributes);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
    }
    
    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }
        
        foreach (var attributeData in methodSymbol.GetAttributes())
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
}