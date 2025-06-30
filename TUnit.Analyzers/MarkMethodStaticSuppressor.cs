using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MarkMethodStaticSuppressor : DiagnosticSuppressor
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

            if (methodSymbol.IsTestMethod(context.Compilation))
            {
                Suppress(context, diagnostic);
            }
            else if (methodSymbol.IsStandardHookMethod(context.Compilation, out _, out var hookLevel, out _)
                     && hookLevel is HookLevel.Test)
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
        new()
        {
            CreateDescriptor("CA1822")
        };

    private static SuppressionDescriptor CreateDescriptor(string id)
        => new(
            id: $"{id}Suppression",
            suppressedDiagnosticId: id,
            justification: $"Suppress {id} for TUnit instance methods."
        );
}
