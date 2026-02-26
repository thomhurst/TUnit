using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Mocks.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DelegateMockAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.TM003_OfDelegateRequiresDelegateType);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);

        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (!IsOfDelegateMethod(methodSymbol))
        {
            return;
        }

        if (methodSymbol.TypeArguments.Length != 1)
        {
            return;
        }

        var typeArgument = methodSymbol.TypeArguments[0];

        // Skip open type parameters (e.g. inside the Mock.OfDelegate<T>() implementation itself)
        if (typeArgument is ITypeParameterSymbol)
        {
            return;
        }

        if (typeArgument.TypeKind != TypeKind.Delegate)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.TM003_OfDelegateRequiresDelegateType,
                    invocation.GetLocation(),
                    typeArgument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                )
            );
        }
    }

    private static bool IsOfDelegateMethod(IMethodSymbol method)
    {
        return method.Name is "OfDelegate"
               && method.ContainingType is { Name: "Mock", ContainingNamespace: { Name: "Mocks", ContainingNamespace: { Name: "TUnit", ContainingNamespace.IsGlobalNamespace: true } } }
               && method.IsStatic
               && method.IsGenericMethod;
    }
}
