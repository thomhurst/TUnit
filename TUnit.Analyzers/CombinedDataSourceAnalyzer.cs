using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

/// <summary>
/// Analyzer for CombinedDataSources validation
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CombinedDataSourceAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.CombinedDataSourceAttributeRequired,
            Rules.CombinedDataSourceMissingParameterDataSource,
            Rules.CombinedDataSourceConflictWithMatrix
        );

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
    }

    private void AnalyzeClass(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        if (!namedTypeSymbol.IsTestClass(context.Compilation))
        {
            return;
        }

        CheckCombinedDataSourceErrors(context, namedTypeSymbol.GetAttributes(),
            namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty);
    }

    private void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (!methodSymbol.IsTestMethod(context.Compilation))
        {
            return;
        }

        CheckCombinedDataSourceErrors(context, methodSymbol.GetAttributes(), methodSymbol.Parameters);
    }

    private void CheckCombinedDataSourceErrors(SymbolAnalysisContext context,
        ImmutableArray<AttributeData> attributes,
        ImmutableArray<IParameterSymbol> parameters)
    {
        var hasCombinedDataSource = attributes.Any(x =>
            x.IsCombinedDataSourceAttribute(context.Compilation));

        var parametersWithDataSources = parameters
            .Where(p => p.HasDataSourceAttribute(context.Compilation))
            .ToList();

        // Rule 1: If parameters have data source attributes, CombinedDataSources must be present
        if (parametersWithDataSources.Any() && !hasCombinedDataSource)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.CombinedDataSourceAttributeRequired,
                    context.Symbol.Locations.FirstOrDefault())
            );
        }

        // Rule 2: If CombinedDataSources is present, all parameters must have data sources
        if (hasCombinedDataSource)
        {
            // Filter out CancellationToken parameters as they're handled by the engine
            var nonCancellationTokenParams = parameters
                .Where(p => p.Type.GloballyQualifiedNonGeneric() !=
                           "global::System.Threading.CancellationToken")
                .ToList();

            foreach (var parameter in nonCancellationTokenParams)
            {
                if (!parameter.HasDataSourceAttribute(context.Compilation))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(Rules.CombinedDataSourceMissingParameterDataSource,
                            parameter.Locations.FirstOrDefault() ?? context.Symbol.Locations.FirstOrDefault(),
                            parameter.Name)
                    );
                }
            }

            // Rule 3: Warn if mixing CombinedDataSources with MatrixDataSource
            var hasMatrixDataSource = attributes.Any(x => x.IsMatrixDataSourceAttribute(context.Compilation));
            if (hasMatrixDataSource)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.CombinedDataSourceConflictWithMatrix,
                        context.Symbol.Locations.FirstOrDefault())
                );
            }
        }
    }
}
