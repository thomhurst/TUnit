using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ClassParametersAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create
        (
            Rules.NoDataSourceProvided
        );

    protected override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }
    
    private void AnalyzeSymbol(SymbolAnalysisContext context)
    { 
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        if (namedTypeSymbol.InstanceConstructors.Length == 0 ||
            namedTypeSymbol.InstanceConstructors.All(x => x.Parameters.Length == 0))
        {
            return;
        }

        if (!namedTypeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Any(x => x.IsTestMethod()))
        {
            return;
        }
        
        if (!namedTypeSymbol.HasDataDrivenAttributes())
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.NoDataSourceProvided, namedTypeSymbol.Locations.FirstOrDefault()));
        }
    }
}