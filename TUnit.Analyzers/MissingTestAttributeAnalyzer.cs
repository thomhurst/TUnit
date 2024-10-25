using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingTestAttributeAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.MissingTestAttribute);

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

        var methods = namedTypeSymbol
            .GetSelfAndBaseTypes()
            .SelectMany(x => x.GetMembers())
            .OfType<IMethodSymbol>()
            .Where(x => x.MethodKind == MethodKind.Ordinary)
            .Where(x => !x.IsStatic);

        foreach (var method in methods.Where(x => x.HasDataDrivenAttributes() && !x.IsTestMethod()))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MissingTestAttribute,
                method.Locations.FirstOrDefault())
            );
        }
    }
}