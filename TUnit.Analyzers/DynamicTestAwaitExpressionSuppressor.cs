using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DynamicTestAwaitExpressionSuppressor : DiagnosticSuppressor
{
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (diagnostic.Location.SourceTree?.GetRoot().FindNode(diagnostic.Location.SourceSpan) is not InvocationExpressionSyntax invocationExpressionSyntax)
            {
                continue;
            }

            if (GetParentObjectInitializerSyntax(invocationExpressionSyntax) is not { } objectCreationExpressionSyntax)
            {
                continue;
            }

            var semanticModel = context.GetSemanticModel(objectCreationExpressionSyntax.Type.SyntaxTree);

            if (semanticModel.GetSymbolInfo(objectCreationExpressionSyntax.Type).Symbol is not INamedTypeSymbol namedTypeSymbol)
            {
                continue;
            }

            if (namedTypeSymbol.Name == "DynamicTest" || namedTypeSymbol.Name == "DynamicTest")
            {
                Suppress(context, diagnostic);
            }
        }
    }

    private ObjectCreationExpressionSyntax? GetParentObjectInitializerSyntax(InvocationExpressionSyntax invocationExpressionSyntax)
    {
        var parent = invocationExpressionSyntax.Parent;

        while (parent is not null)
        {
            if (parent is ObjectCreationExpressionSyntax objectCreationExpressionSyntax)
            {
                return objectCreationExpressionSyntax;
            }

            parent = parent.Parent;
        }

        return null;
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
        ImmutableArray.Create(CreateDescriptor("CA2012"), CreateDescriptor("CS4014"));

    private static SuppressionDescriptor CreateDescriptor(string id)
        => new(
            id: $"{id}Suppression",
            suppressedDiagnosticId: id,
            justification: $"Suppress {id} for Dynamic Test expression."
        );
}
