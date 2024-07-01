using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DependsOnNotInParallelConflictAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.DependsOnNotInParallelConflict);

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

        if (!methodSymbol.IsTestMethod())
        {
            return;
        }

        var attributes = methodSymbol.GetAttributes();

        if (attributes.Any(x =>
                x.AttributeClass?.IsOrInherits(WellKnown.AttributeFullyQualifiedClasses.NotInParallelAttribute) == true)
            && attributes.Any(x =>
                x.AttributeClass?.IsOrInherits(WellKnown.AttributeFullyQualifiedClasses.DependsOnAttribute) == true))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.DependsOnNotInParallelConflict, methodSymbol.Locations.FirstOrDefault()));
        }
    }
}