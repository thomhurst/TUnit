using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Assertions.Analyzers.Extensions;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// A sample analyzer that reports the company name being used in class declarations.
/// Traverses through the Syntax Tree and checks the name (identifier) of each class node.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AwaitAssertionAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.AwaitAssertion, Rules.DisposableUsingMultiple);

    public override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SimpleMemberAccessExpression);
    }
    
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(memberAccessExpressionSyntax);

        if (symbol.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        var fullyQualifiedNonGenericMethodName = methodSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
        if (fullyQualifiedNonGenericMethodName 
            is not "global::TUnit.Assertions.Assert.That"
            and not "global::TUnit.Assertions.Assert.Multiple")
        {
            return;
        }

        var expressionStatementParent = memberAccessExpressionSyntax.GetAncestorSyntaxOfType<ExpressionStatementSyntax>();

        if (expressionStatementParent is null)
        {
            return;
        }
        
        if(fullyQualifiedNonGenericMethodName is "global::TUnit.Assertions.Assert.Multiple"
            && !expressionStatementParent.DescendantNodes().Any(x => x is UsingStatementSyntax))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.DisposableUsingMultiple, expressionStatementParent.GetLocation())
            );
            return;
        }
        
        if (expressionStatementParent.DescendantNodes().Any(x => x is AwaitExpressionSyntax))
        {
            return;
        }
        
        context.ReportDiagnostic(
            Diagnostic.Create(Rules.AwaitAssertion, expressionStatementParent.GetLocation())
        );
    }
}