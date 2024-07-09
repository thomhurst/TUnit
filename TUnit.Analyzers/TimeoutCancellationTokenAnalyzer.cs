using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TimeoutCancellationTokenAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.MissingTimeoutCancellationTokenAttributes);

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

        var attributes = methodSymbol.GetAttributes()
            .Concat(methodSymbol.ContainingType.GetAttributes());

        var timeoutAttribute = attributes.FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                             == "global::TUnit.Core.TimeoutAttribute");

        if (timeoutAttribute is null)
        {
            return;
        }

        var parameters = methodSymbol.Parameters;

        if (parameters.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.MissingTimeoutCancellationTokenAttributes,
                    context.Symbol.Locations.FirstOrDefault())
            );
            return;
        }
        
        var lastParameter = parameters.Last();
        
        if (!SymbolEqualityComparer.Default.Equals(lastParameter.Type,
                context.Compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.MissingTimeoutCancellationTokenAttributes,
                    context.Symbol.Locations.FirstOrDefault())
            );
        }
    }
}