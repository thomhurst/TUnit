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
            return convertedAssertion;
        }
        
        return base.VisitInvocationExpression(node);
    }
    
    protected abstract InvocationExpressionSyntax? ConvertAssertionIfNeeded(InvocationExpressionSyntax invocation);
    
    protected InvocationExpressionSyntax CreateTUnitAssertion(
        string methodName,
        ExpressionSyntax actualValue,
        params ArgumentSyntax[] additionalArguments)
    {
        var awaitExpression = SyntaxFactory.AwaitExpression(
            SyntaxFactory.InvocationExpression(
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
            )
        );
        
        var methodAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            awaitExpression,
            SyntaxFactory.IdentifierName(methodName)
        );
        
        var arguments = additionalArguments.Length > 0
            ? SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(additionalArguments))
            : SyntaxFactory.ArgumentList();
        
        return SyntaxFactory.InvocationExpression(methodAccess, arguments);
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