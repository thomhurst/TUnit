using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestMethodParametersAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.NoDataSourceProvided, Rules.UnsupportedSpanParams);

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

        foreach (var parameter in methodSymbol.Parameters)
        {
            if (parameter.IsParams && parameter.Type is INamedTypeSymbol type
                && (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition,
                        context.Compilation.GetTypeByMetadataName("System.ReadOnlySpan`1"))
                    || SymbolEqualityComparer.Default.Equals(type.OriginalDefinition,
                        context.Compilation.GetTypeByMetadataName("System.Span`1"))))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.UnsupportedSpanParams,
                    parameter.Locations.FirstOrDefault(), parameter.Name));
            }
        }

        var parameters = methodSymbol.Parameters.WithoutTestMethodCancellationTokens().ToArray();

        if (parameters.Length == 0)
        {
            return;
        }

        if (!methodSymbol.HasDataDrivenAttributes())
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.NoDataSourceProvided,
                parameters[0].Locations.FirstOrDefault() ?? methodSymbol.Locations.FirstOrDefault()));
        }
    }
}
