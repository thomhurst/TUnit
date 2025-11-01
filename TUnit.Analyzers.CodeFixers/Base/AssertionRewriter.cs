using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers.Base;

public abstract class AssertionRewriter : CSharpSyntaxRewriter
{
    protected readonly SemanticModel SemanticModel;
    protected abstract string FrameworkName { get; }
    
    protected AssertionRewriter(SemanticModel semanticModel)
    {
        SemanticModel = semanticModel;
    }
    
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var convertedAssertion = ConvertAssertionIfNeeded(node);
        if (convertedAssertion != null)
        {
            // Preserve the original trivia (whitespace, comments, etc.)
            return convertedAssertion
                .WithLeadingTrivia(node.GetLeadingTrivia())
                .WithTrailingTrivia(node.GetTrailingTrivia());
        }

        return base.VisitInvocationExpression(node);
    }
    
    protected abstract ExpressionSyntax? ConvertAssertionIfNeeded(InvocationExpressionSyntax invocation);

    protected ExpressionSyntax CreateTUnitAssertion(
        string methodName,
        ExpressionSyntax actualValue,
        params ArgumentSyntax[] additionalArguments)
    {
        // Create Assert.That(actualValue)
        var assertThatInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.IdentifierName("That")
            ),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(actualValue)
                )
            )
        );

        // Create Assert.That(actualValue).MethodName(args)
        var methodAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            assertThatInvocation,
            SyntaxFactory.IdentifierName(methodName)
        );

        var arguments = additionalArguments.Length > 0
            ? SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(additionalArguments))
            : SyntaxFactory.ArgumentList();

        var fullInvocation = SyntaxFactory.InvocationExpression(methodAccess, arguments);

        // Now wrap the entire thing in await: await Assert.That(actualValue).MethodName(args)
        return SyntaxFactory.AwaitExpression(fullInvocation);
    }
    
    protected bool IsFrameworkAssertion(InvocationExpressionSyntax invocation)
    {
        var symbol = SemanticModel.GetSymbolInfo(invocation).Symbol;
        if (symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }
        
        var namespaceName = methodSymbol.ContainingNamespace?.ToDisplayString() ?? "";
        return IsFrameworkAssertionNamespace(namespaceName);
    }
    
    protected abstract bool IsFrameworkAssertionNamespace(string namespaceName);
}