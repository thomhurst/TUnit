using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Mock.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SealedClassMockAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.TM001_CannotMockSealedType);

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

        if (!IsMockOfMethod(methodSymbol))
        {
            return;
        }

        if (methodSymbol.TypeArguments.Length != 1)
        {
            return;
        }

        var typeArgument = methodSymbol.TypeArguments[0];

        if (typeArgument is INamedTypeSymbol namedType && namedType.IsSealed && namedType.TypeKind == TypeKind.Class)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.TM001_CannotMockSealedType,
                    invocation.GetLocation(),
                    typeArgument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                )
            );
        }
    }

    private static bool IsMockOfMethod(IMethodSymbol method)
    {
        return method.Name == "Of"
               && method.ContainingType is { Name: "Mock", ContainingNamespace: { Name: "Mock", ContainingNamespace: { Name: "TUnit", ContainingNamespace.IsGlobalNamespace: true } } }
               && method.IsStatic
               && method.IsGenericMethod;
    }
}
