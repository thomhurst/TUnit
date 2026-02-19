using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TimeoutCancellationTokenAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.MissingTimeoutCancellationTokenAttributes,
            Rules.CancellationTokenMustBeLastParameter);

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

        if (!methodSymbol.IsTestMethod(context.Compilation) &&
            !methodSymbol.IsHookMethod(context.Compilation, out _, out _, out _))
        {
            return;
        }

        var attributes = methodSymbol.GetAttributes()
            .Concat(methodSymbol.ContainingType.GetAttributes());

        var timeoutAttribute = attributes.FirstOrDefault(x => x.AttributeClass?.GloballyQualifiedNonGeneric()
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

        var cancellationTokenType = context.Compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!);

        var cancellationTokenIndex = -1;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (SymbolEqualityComparer.Default.Equals(parameters[i].Type, cancellationTokenType))
            {
                cancellationTokenIndex = i;
                break;
            }
        }

        if (cancellationTokenIndex == -1)
        {
            // CancellationToken is not present at all
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.MissingTimeoutCancellationTokenAttributes,
                    lastParameter.Locations.FirstOrDefault() ?? context.Symbol.Locations.FirstOrDefault())
            );
        }
        else if (cancellationTokenIndex != parameters.Length - 1)
        {
            // CancellationToken exists but is not the last parameter
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.CancellationTokenMustBeLastParameter,
                    context.Symbol.Locations.FirstOrDefault())
            );
        }
    }
}
