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
            if (diagnostic.Location.SourceTree?.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan) is not { } node)
            {
                continue;
            }

            var semanticModel = context.GetSemanticModel(diagnostic.Location.SourceTree);

            if (semanticModel.GetDeclaredSymbol(node) is not IMethodSymbol methodSymbol)
            {
                continue;
            }

            // HasTestAttribute is inheritance-aware (covers [Test], [DynamicTestBuilder], etc.) and
            // IsHookMethod covers all hook levels (Before/After/BeforeEvery/AfterEvery). This is
            // deliberately broader than MarkMethodStaticSuppressor (CA1822), which uses the exact-match
            // IsTestMethod and only narrows to Test-level hooks — so a [DynamicTestBuilder] method may
            // still see CA1822. That divergence is intentional: VSTHRD200 (async naming) never applies
            // to any test/hook method, whereas CA1822 (make-static) has narrower, scope-specific intent.
            if (methodSymbol.HasTestAttribute(context.Compilation)
                || methodSymbol.IsHookMethod(context.Compilation, out _, out _, out _))
            {
                Suppress(context, diagnostic);
            }
        }
    }

    // Exactly one descriptor is registered (VSTHRD200), and Roslyn only routes diagnostics whose id
    // matches it here, so referencing the single descriptor directly is correct and avoids a
    // per-call predicate scan.
    private static void Suppress(SuppressionAnalysisContext context, Diagnostic diagnostic)
        => context.ReportSuppression(Suppression.Create(Vsthrd200Descriptor, diagnostic));

    private static readonly SuppressionDescriptor Vsthrd200Descriptor = CreateDescriptor("VSTHRD200");

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
        ImmutableArray.Create(Vsthrd200Descriptor);

    private static SuppressionDescriptor CreateDescriptor(string id)
        => new(
            id: $"{id}Suppression",
            suppressedDiagnosticId: id,
            justification: "TUnit test and hook methods do not require the 'Async' suffix."
        );
}
