using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

/// <summary>
/// Analyzer for MixedParametersDataSource validation
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MixedParametersDataSourceAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.MixedParametersDataSourceAttributeRequired,
            Rules.MixedParametersDataSourceMissingParameterDataSource,
            Rules.MixedParametersDataSourceConflictWithMatrix
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

        CheckMixedParametersDataSourceErrors(context, namedTypeSymbol.GetAttributes(),
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

        CheckMixedParametersDataSourceErrors(context, methodSymbol.GetAttributes(), methodSymbol.Parameters);
    }

    private void CheckMixedParametersDataSourceErrors(SymbolAnalysisContext context,
        ImmutableArray<AttributeData> attributes,
        ImmutableArray<IParameterSymbol> parameters)
    {
        var hasMixedParametersDataSource = attributes.Any(x =>
            x.IsMixedParametersDataSourceAttribute(context.Compilation));

        var parametersWithDataSources = parameters
            .Where(p => p.HasDataSourceAttribute(context.Compilation))
            .ToList();

        // Rule 1: If parameters have data source attributes, MixedParametersDataSource must be present
        if (parametersWithDataSources.Any() && !hasMixedParametersDataSource)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.MixedParametersDataSourceAttributeRequired,
                    context.Symbol.Locations.FirstOrDefault())
            );
        }

        // Rule 2: If MixedParametersDataSource is present, all parameters must have data sources
        if (hasMixedParametersDataSource)
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
                        Diagnostic.Create(Rules.MixedParametersDataSourceMissingParameterDataSource,
                            parameter.Locations.FirstOrDefault() ?? context.Symbol.Locations.FirstOrDefault(),
                            parameter.Name)
                    );
                }
            }

            // Rule 3: Warn if mixing MixedParametersDataSource with MatrixDataSource
            var hasMatrixDataSource = attributes.Any(x => x.IsMatrixDataSourceAttribute(context.Compilation));
            if (hasMatrixDataSource)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.MixedParametersDataSourceConflictWithMatrix,
                        context.Symbol.Locations.FirstOrDefault())
                );
            }
        }
    }
}
