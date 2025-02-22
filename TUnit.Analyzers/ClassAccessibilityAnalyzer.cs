using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ClassAccessibilityAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create
        (
            Rules.MethodMustBePublic,
            Rules.TypeMustBePublic
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

        if (namedTypeSymbol.DeclaredAccessibility == Accessibility.Public)
        {
            return;
        }

        var compilation = context.Compilation;

        var testsOrHooks = namedTypeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(x => x.IsTestMethod(compilation) || x.IsHookMethod(compilation, out _, out _, out _))
            .ToArray();
        
        if (!testsOrHooks.Any())
        {
            return;
        }
        
        foreach (var methodSymbol in testsOrHooks.Where(x => x.DeclaredAccessibility != Accessibility.Public))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustBePublic, methodSymbol.Locations.FirstOrDefault()));
        }

        if (namedTypeSymbol.ContainingType != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.TypeMustBePublic, namedTypeSymbol.Locations.FirstOrDefault()));
        }
    }
}