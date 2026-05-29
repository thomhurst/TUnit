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

            // IsHookMethod covers all hook levels (Before/After/BeforeEvery/AfterEvery) intentionally —
            // no hook method requires the 'Async' suffix regardless of scope. (This is deliberately
            // broader than MarkMethodStaticSuppressor, which only narrows CA1822 to Test-level hooks.)
            if (methodSymbol.HasTestAttribute(context.Compilation)
                || methodSymbol.IsHookMethod(context.Compilation, out _, out _, out _))
            {
                Suppress(context, diagnostic);
            }
        }
    }

    // This suppressor only ever handles VSTHRD200, so reference the single descriptor directly
    // rather than scanning SupportedSuppressions on every reported diagnostic.
    private void Suppress(SuppressionAnalysisContext context, Diagnostic diagnostic)
        => context.ReportSuppression(Suppression.Create(SupportedSuppressions[0], diagnostic));

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
        ImmutableArray.Create(CreateDescriptor("VSTHRD200"));

    private static SuppressionDescriptor CreateDescriptor(string id)
        => new(
            id: $"{id}Suppression",
            suppressedDiagnosticId: id,
            justification: "TUnit test and hook methods do not require the 'Async' suffix."
        );
}
