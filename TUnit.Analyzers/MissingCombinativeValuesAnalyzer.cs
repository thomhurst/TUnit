using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingMatrixAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.NoTestDataProvided);

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

        if (!methodSymbol.GetAttributes().Any(x =>
                x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                == "global::TUnit.Core.MatrixTestAttribute"))
        {
            return;
        }

        var parameters = methodSymbol.Parameters.IsDefaultOrEmpty
            ? []
            : methodSymbol.Parameters.ToList();

        if (!parameters.Any())
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.NoTestDataProvided,
                    context.Symbol.Locations.FirstOrDefault())
            );
            return;
        }
        
        if (methodSymbol.HasTimeoutAttribute(out _)
            && SymbolEqualityComparer.Default.Equals(parameters.LastOrDefault()?.Type,
                context.Compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)))
        {
            parameters.RemoveAt(parameters.Count - 1);
        }
        
        foreach (var parameterSymbol in parameters)
        {
            var matrixAttribute = parameterSymbol.GetAttributes().FirstOrDefault(attribute =>
                attribute.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                == WellKnown.AttributeFullyQualifiedClasses.Matrix.WithGlobalPrefix);

            if (matrixAttribute is null
                || matrixAttribute.ConstructorArguments.IsDefaultOrEmpty
                || matrixAttribute.ConstructorArguments[0].Values.IsDefaultOrEmpty)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.NoTestDataProvided,
                        parameterSymbol.Locations.FirstOrDefault())
                );
            }
        }
    }
}