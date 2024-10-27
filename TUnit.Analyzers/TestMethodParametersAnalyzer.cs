using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestMethodParametersAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create
        (
            Rules.NoDataSourceProvided
        );

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

        if (methodSymbol.IsAbstract || !methodSymbol.IsTestMethod(context.Compilation))
        {
            return;
        }

        var parameters = methodSymbol.Parameters.WithoutTimeoutParameter().ToArray();
        
        if (parameters.Length == 0)
        {
            return;
        }
        
        if (!methodSymbol.HasDataDrivenAttributes())
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.NoDataSourceProvided, methodSymbol.Locations.FirstOrDefault()));
        }
    }
}