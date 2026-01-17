using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers;

/// <summary>
/// Converts Assert.Multiple(() => { ... }) to using (Assert.Multiple()) { ... }
/// This is a syntax-only transformation that doesn't require a semantic model.
/// </summary>
public class NUnitAssertMultipleRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        // Check if this is Assert.Multiple(() => { ... })
        if (node.Expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Assert" } &&
            memberAccess.Name.Identifier.Text == "Multiple" &&
            invocation.ArgumentList.Arguments.Count == 1)
        {
            var argument = invocation.ArgumentList.Arguments[0].Expression;

            // Handle lambda: Assert.Multiple(() => { ... })
            if (argument is ParenthesizedLambdaExpressionSyntax lambda)
            {
                return ConvertAssertMultipleLambda(node, lambda);
            }

            // Handle simple lambda: Assert.Multiple(() => expr)
            if (argument is SimpleLambdaExpressionSyntax simpleLambda)
            {
                return ConvertAssertMultipleSimpleLambda(node, simpleLambda);
            }
        }

        return base.VisitExpressionStatement(node);
    }

    private SyntaxNode ConvertAssertMultipleLambda(ExpressionStatementSyntax originalStatement, ParenthesizedLambdaExpressionSyntax lambda)
    {
        // Extract statements from lambda body
        SyntaxList<StatementSyntax> statements;
        if (lambda.Body is BlockSyntax block)
        {
            // Visit each statement to convert inner assertions
            var convertedStatements = block.Statements.Select(s => (StatementSyntax)Visit(s)!).ToArray();
            statements = SyntaxFactory.List(convertedStatements);
        }
        else if (lambda.Body is ExpressionSyntax expr)
        {
            // Single expression lambda - convert it
            var visitedExpr = (ExpressionSyntax)Visit(expr)!;
            statements = SyntaxFactory.SingletonList<StatementSyntax>(
                SyntaxFactory.ExpressionStatement(visitedExpr));
        }
        else
        {
            return originalStatement;
        }

        return CreateUsingMultipleStatement(originalStatement, statements);
    }

    private SyntaxNode ConvertAssertMultipleSimpleLambda(ExpressionStatementSyntax originalStatement, SimpleLambdaExpressionSyntax lambda)
    {
        SyntaxList<StatementSyntax> statements;
        if (lambda.Body is BlockSyntax block)
        {
            var convertedStatements = block.Statements.Select(s => (StatementSyntax)Visit(s)!).ToArray();
            statements = SyntaxFactory.List(convertedStatements);
        }
        else if (lambda.Body is ExpressionSyntax expr)
        {
            var visitedExpr = (ExpressionSyntax)Visit(expr)!;
            statements = SyntaxFactory.SingletonList<StatementSyntax>(
                SyntaxFactory.ExpressionStatement(visitedExpr));
        }
        else
        {
            return originalStatement;
        }

        return CreateUsingMultipleStatement(originalStatement, statements);
    }

    private UsingStatementSyntax CreateUsingMultipleStatement(ExpressionStatementSyntax originalStatement, SyntaxList<StatementSyntax> statements)
    {
        // Create: Assert.Multiple()
        var assertMultipleInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.IdentifierName("Multiple")),
            SyntaxFactory.ArgumentList());

        // Create the using statement: using (Assert.Multiple()) { ... }
        var usingStatement = SyntaxFactory.UsingStatement(
            declaration: null,
            expression: assertMultipleInvocation,
            statement: SyntaxFactory.Block(statements)
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(SyntaxFactory.LineFeed))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(originalStatement.GetLeadingTrivia())));

        return usingStatement
            .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space))
            .WithOpenParenToken(SyntaxFactory.Token(SyntaxKind.OpenParenToken))
            .WithCloseParenToken(SyntaxFactory.Token(SyntaxKind.CloseParenToken))
            .WithLeadingTrivia(originalStatement.GetLeadingTrivia())
            .WithTrailingTrivia(originalStatement.GetTrailingTrivia());
    }
}
