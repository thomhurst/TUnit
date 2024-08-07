using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PublicMethodMissingTestAttributeAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create
        (
            Rules.PublicMethodMissingTestAttribute
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

        var methods = namedTypeSymbol.GetMembers().OfType<IMethodSymbol>().ToArray();
        
        if (!methods.Any(x => x.IsTestMethod()))
        {
            return;
        }
        
        foreach (var method in methods
                     .Where(x => x.MethodKind == MethodKind.Ordinary)
                     .Where(x => !x.IsStatic)
                     .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                     .Where(x => !x.IsTestMethod())
                     .Where(x => !x.IsHookMethod()))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.PublicMethodMissingTestAttribute, method.Locations.FirstOrDefault()));
        }
    }
}