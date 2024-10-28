using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConflictingExplicitAttributesAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.ConflictingExplicitAttributes);

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

        var methodExplicitAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
            == WellKnown.AttributeFullyQualifiedClasses.Explicit.WithGlobalPrefix);

        if (methodExplicitAttribute == null)
        {
            return;
        }
        
        var classExplicitAttribute = methodSymbol.ContainingType.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                 == WellKnown.AttributeFullyQualifiedClasses.Explicit.WithGlobalPrefix);
        
        if (classExplicitAttribute == null)
        {
            return;
        }
        
        context.ReportDiagnostic(
                Diagnostic.Create(Rules.ConflictingExplicitAttributes,
                    methodExplicitAttribute.GetLocation()
                    ?? context.Symbol.Locations.FirstOrDefault())
            );
    }
}