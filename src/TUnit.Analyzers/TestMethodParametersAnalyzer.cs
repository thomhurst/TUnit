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
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var readOnlySpanType = compilationContext.Compilation.GetTypeByMetadataName("System.ReadOnlySpan`1");
            var spanType = compilationContext.Compilation.GetTypeByMetadataName("System.Span`1");
            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeSymbol(symbolContext, readOnlySpanType, spanType), SymbolKind.Method);
        });
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol? readOnlySpanType, INamedTypeSymbol? spanType)
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
            if (parameter.IsParams && IsSpanType(parameter.Type, readOnlySpanType, spanType))
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

    private static bool IsSpanType(ITypeSymbol type, INamedTypeSymbol? readOnlySpanType, INamedTypeSymbol? spanType)
    {
        return type is INamedTypeSymbol namedType
            && (SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, readOnlySpanType)
                || SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, spanType));
    }
}
