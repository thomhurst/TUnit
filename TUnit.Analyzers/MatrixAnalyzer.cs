using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MatrixAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        Rules.MatrixDataSourceAttributeRequired, Rules.WrongArgumentTypeTestData
    ];

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

        foreach (var parameterSymbol in parameters)
        {
            var matrixAttribute = parameterSymbol.GetAttributes().FirstOrDefault(x => x.IsMatrixAttribute(context.Compilation));

            if (matrixAttribute is null
                or { ConstructorArguments.IsDefaultOrEmpty: true })
            {
                continue;
            }

            var arrayArgument = matrixAttribute.ConstructorArguments[0];

            if (arrayArgument.Kind != TypedConstantKind.Array)
            {
                continue;
            }

            foreach (var arrayItem in arrayArgument.Values)
            {
                if (arrayItem.Type is null)
                {
                    continue;
                }

                var conversion = context.Compilation.ClassifyConversion(arrayItem.Type, parameterSymbol.Type);

                if (!conversion.Exists)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                        context.Symbol.Locations.FirstOrDefault(),
                        arrayItem.Type,
                        parameterSymbol.Type));

                    return;
                }
            }
        }
    }

}
