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
            var conversionTrivia = convertedAssertion.GetLeadingTrivia();
            var originalTrivia = node.GetLeadingTrivia();

            SyntaxTriviaList finalTrivia;
            // Only do special handling when there's actually a TODO comment
            var hasComment = conversionTrivia.Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia));
            if (hasComment)
            {
                // Conversion added trivia (TODO comments). Structure should be:
                // [original whitespace] [TODO comment] [newline] [original whitespace] [await expression]
                var whitespaceTrivia = originalTrivia.Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToList();
                var nonWhitespaceTrivia = originalTrivia.Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)).ToList();

                var builder = new List<SyntaxTrivia>();
                builder.AddRange(nonWhitespaceTrivia); // Add any non-whitespace (e.g., leading newlines)
                builder.AddRange(whitespaceTrivia);    // Add indentation
                builder.AddRange(conversionTrivia);    // Add TODO comment + newline
                builder.AddRange(whitespaceTrivia);    // Add indentation again for the await

                finalTrivia = new SyntaxTriviaList(builder);
            }
            else
            {
                // No TODO comment, just use original trivia
                finalTrivia = originalTrivia;
            }

            return convertedAssertion
                .WithLeadingTrivia(finalTrivia)
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
        return CreateTUnitAssertionWithMessage(methodName, actualValue, null, additionalArguments);
    }

    protected ExpressionSyntax CreateTUnitAssertionWithMessage(
        string methodName,
        ExpressionSyntax actualValue,
        ExpressionSyntax? message,
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

        ExpressionSyntax fullInvocation = SyntaxFactory.InvocationExpression(methodAccess, arguments);

        // Add .Because(message) if message is provided and non-empty
        if (message != null && !IsEmptyOrNullMessage(message))
        {
            var becauseAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                fullInvocation,
                SyntaxFactory.IdentifierName("Because")
            );

            fullInvocation = SyntaxFactory.InvocationExpression(
                becauseAccess,
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(message)
                    )
                )
            );
        }

        // Now wrap the entire thing in await: await Assert.That(actualValue).MethodName(args).Because(message)
        // Need to add a trailing space after 'await' keyword
        var awaitKeyword = SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);
        return SyntaxFactory.AwaitExpression(awaitKeyword, fullInvocation);
    }

    private static bool IsEmptyOrNullMessage(ExpressionSyntax message)
    {
        // Check for null literal
        if (message is LiteralExpressionSyntax literal)
        {
            if (literal.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return true;
            }

            // Check for empty string literal
            if (literal.IsKind(SyntaxKind.StringLiteralExpression) &&
                literal.Token.ValueText == "")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Extracts the message and any format arguments from an argument list.
    /// Format string messages like Assert.AreEqual(5, x, "Expected {0}", x) have args after the message.
    /// </summary>
    protected static (ExpressionSyntax? message, ArgumentSyntax[]? formatArgs) ExtractMessageWithFormatArgs(
        SeparatedSyntaxList<ArgumentSyntax> arguments,
        int messageIndex)
    {
        if (arguments.Count <= messageIndex)
        {
            return (null, null);
        }

        var message = arguments[messageIndex].Expression;

        // Check if there are additional format arguments after the message
        if (arguments.Count > messageIndex + 1)
        {
            var formatArgs = arguments.Skip(messageIndex + 1).ToArray();
            return (message, formatArgs);
        }

        return (message, null);
    }

    /// <summary>
    /// Creates a message expression, wrapping in string.Format if format args are present.
    /// </summary>
    protected static ExpressionSyntax CreateMessageExpression(
        ExpressionSyntax message,
        ArgumentSyntax[]? formatArgs)
    {
        if (formatArgs == null || formatArgs.Length == 0)
        {
            return message;
        }

        // Create string.Format(message, arg1, arg2, ...)
        var allArgs = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(message)
        };
        allArgs.AddRange(formatArgs);

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                SyntaxFactory.IdentifierName("Format")
            ),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(allArgs))
        );
    }

    /// <summary>
    /// Checks if the argument at the given index appears to be a comparer (IComparer, IEqualityComparer).
    /// </summary>
    protected bool IsLikelyComparerArgument(ArgumentSyntax argument)
    {
        var typeInfo = SemanticModel.GetTypeInfo(argument.Expression);
        if (typeInfo.Type == null) return false;

        var typeName = typeInfo.Type.ToDisplayString();

        // Check for IComparer, IComparer<T>, IEqualityComparer, IEqualityComparer<T>
        if (typeName.Contains("IComparer") || typeName.Contains("IEqualityComparer"))
        {
            return true;
        }

        // Check interfaces
        if (typeInfo.Type is INamedTypeSymbol namedType)
        {
            return namedType.AllInterfaces.Any(i =>
                i.Name == "IComparer" ||
                i.Name == "IEqualityComparer");
        }

        return false;
    }

    /// <summary>
    /// Creates a TODO comment for unsupported features during migration.
    /// </summary>
    protected static SyntaxTrivia CreateTodoComment(string message)
    {
        return SyntaxFactory.Comment($"// TODO: TUnit migration - {message}");
    }
    
    protected bool IsFrameworkAssertion(InvocationExpressionSyntax invocation)
    {
        var symbolInfo = SemanticModel.GetSymbolInfo(invocation);
        var symbol = symbolInfo.Symbol;

        if (symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        var namespaceName = methodSymbol.ContainingNamespace?.ToDisplayString() ?? "";
        return IsFrameworkAssertionNamespace(namespaceName);
    }
    
    protected abstract bool IsFrameworkAssertionNamespace(string namespaceName);
}