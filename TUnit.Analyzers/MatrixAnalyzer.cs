using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MatrixAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.MatrixDataSourceAttributeRequired);

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

        CheckMatrixErrors(context, namedTypeSymbol.GetAttributes(), namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty);
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

        CheckMatrixErrors(context, methodSymbol.GetAttributes(), methodSymbol.Parameters);
    }
    
    private void CheckMatrixErrors(SymbolAnalysisContext context, ImmutableArray<AttributeData> attributes,
        ImmutableArray<IParameterSymbol> parameters)
    {
        if (!parameters.Any(x => x.HasMatrixAttribute(context.Compilation)))
        {
            return;
        }

        if (!attributes.Any(x => x.IsMatrixDataSourceAttribute(context.Compilation)))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.MatrixDataSourceAttributeRequired,
                    context.Symbol.Locations.FirstOrDefault())
            );
        }
    }

}