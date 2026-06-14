using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

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

        if (methodSymbol is not { IsAsync: true, ReturnsVoid: true })
        {
            return;
        }

        // Only flag `async void` on TUnit test methods and hooks. Other `async void`
        // methods (e.g. an event handler for System.Timers.Timer.Elapsed) are legitimate
        // and must not be flagged just because the project references TUnit. See #6190.
        if (!IsTestOrHookMethod(methodSymbol, context.Compilation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.AsyncVoidMethod,
            methodSymbol.Locations.FirstOrDefault())
        );
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

        if (symbol is not IMethodSymbol { ReturnsVoid: true })
        {
            return;
        }

        // Only flag `async void` lambdas declared inside a TUnit test method or hook,
        // so legitimate async void event-handler lambdas elsewhere aren't flagged. See #6190.
        var enclosingMethod = GetEnclosingMethod(symbol);

        if (enclosingMethod is null || !IsTestOrHookMethod(enclosingMethod, context.Compilation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.AsyncVoidMethod,
            anonymousFunction.GetLocation())
        );
    }

    private static bool IsTestOrHookMethod(IMethodSymbol methodSymbol, Compilation compilation)
    {
        return methodSymbol.HasTestAttribute(compilation)
               || methodSymbol.IsHookMethod(compilation, out _, out _, out _);
    }

    private static IMethodSymbol? GetEnclosingMethod(ISymbol? symbol)
    {
        // Walk out of nested lambdas/anonymous methods to the real enclosing method.
        while (symbol is IMethodSymbol method)
        {
            if (method.MethodKind != MethodKind.AnonymousFunction)
            {
                return method;
            }

            symbol = method.ContainingSymbol;
        }

        return null;
    }
}
