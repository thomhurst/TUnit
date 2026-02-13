using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncVoidAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.AsyncVoidMethod);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSyntaxNodeAction(AnalyzeLambda,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.AnonymousMethodExpression);
    }

    private void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (methodSymbol is { IsAsync: true, ReturnsVoid: true })
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.AsyncVoidMethod,
                methodSymbol.Locations.FirstOrDefault())
            );
        }
    }

    private void AnalyzeLambda(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not AnonymousFunctionExpressionSyntax anonymousFunction)
        {
            return;
        }

        if (!anonymousFunction.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(anonymousFunction).Symbol;

        if (symbol is IMethodSymbol { ReturnsVoid: true })
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.AsyncVoidMethod,
                anonymousFunction.GetLocation())
            );
        }
    }
}
