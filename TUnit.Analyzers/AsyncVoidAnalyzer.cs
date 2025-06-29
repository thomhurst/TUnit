using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncVoidAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        Rules.AsyncVoidMethod
    ];

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeSyntax, SymbolKind.Method);
    }

    private void AnalyzeSyntax(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (methodSymbol is { IsAsync: true, ReturnsVoid: true })
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.AsyncVoidMethod,
                methodSymbol.Locations.FirstOrDefault())
            );
        }
    }
}
