using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

/// <summary>
/// Suppresses VSTHRD200 ("Use 'Async' suffix for async methods") on TUnit test
/// methods and hook methods, which intentionally do not follow the Async naming convention.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class Vsthrd200AsyncSuffixSuppressor : DiagnosticSuppressor
{
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (diagnostic.Location.SourceTree?.GetRoot().FindNode(diagnostic.Location.SourceSpan) is not { } node)
            {
                continue;
            }

            var semanticModel = context.GetSemanticModel(diagnostic.Location.SourceTree);

            if (semanticModel.GetDeclaredSymbol(node) is not IMethodSymbol methodSymbol)
            {
                continue;
            }

            if (methodSymbol.HasTestAttribute(context.Compilation)
                || methodSymbol.IsHookMethod(context.Compilation, out _, out _, out _))
            {
                Suppress(context, diagnostic);
            }
        }
    }

    private void Suppress(SuppressionAnalysisContext context, Diagnostic diagnostic)
    {
        var suppression = SupportedSuppressions.First(s => s.SuppressedDiagnosticId == diagnostic.Id);

        context.ReportSuppression(
            Suppression.Create(
                suppression,
                diagnostic
            )
        );
    }

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
        ImmutableArray.Create(CreateDescriptor("VSTHRD200"));

    private static SuppressionDescriptor CreateDescriptor(string id)
        => new(
            id: $"{id}Suppression",
            suppressedDiagnosticId: id,
            justification: "TUnit test and hook methods do not require the 'Async' suffix."
        );
}
