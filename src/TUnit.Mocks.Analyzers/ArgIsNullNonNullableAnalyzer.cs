using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Mocks.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ArgIsNullNonNullableAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.TM005_ArgIsNullNonNullableValueType);

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

        if (!IsArgNullMethod(methodSymbol))
        {
            return;
        }

        var typeArgument = methodSymbol.TypeArguments[0];

        if (!typeArgument.IsValueType)
        {
            return;
        }

        // Nullable<T> is a value type but IS nullable — only flag non-nullable structs
        if (typeArgument.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                Rules.TM005_ArgIsNullNonNullableValueType,
                invocation.GetLocation(),
                methodSymbol.Name,
                typeArgument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            )
        );
    }

    private static bool IsArgNullMethod(IMethodSymbol method)
    {
        return method.Name is "IsNull" or "IsNotNull"
            && method.IsGenericMethod
            && method.Parameters.Length == 0
            && method.ContainingType is { Name: "Arg", ContainingNamespace: { Name: "Arguments", ContainingNamespace: { Name: "Mocks", ContainingNamespace: { Name: "TUnit", ContainingNamespace.IsGlobalNamespace: true } } } };
    }
}
