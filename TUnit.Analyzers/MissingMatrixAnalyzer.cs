using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingMatrixValuesAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.NoTestDataProvided);

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

        var parameters = methodSymbol.Parameters.IsDefaultOrEmpty
            ? []
            : methodSymbol.Parameters.ToList();

        if (!parameters.Any(x => x.HasMatrixAttribute(context.Compilation)))
        {
            return;
        }
        
        foreach (var parameterSymbol in parameters)
        {
            if (SymbolEqualityComparer.Default.Equals(parameters.LastOrDefault()?.Type,
                    context.Compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)))
            {
                continue;
            }
            
            if (!parameterSymbol.HasMatrixAttribute(context.Compilation))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.NoTestDataProvided,
                        context.Symbol.Locations.FirstOrDefault())
                );
            }
        }
    }
}