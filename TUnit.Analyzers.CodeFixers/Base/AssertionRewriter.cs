using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers.Base;

public abstract class AssertionRewriter : CSharpSyntaxRewriter
{
    protected readonly SemanticModel SemanticModel;
    protected abstract string FrameworkName { get; }

    /// <summary>
    /// Tracks whether the current method has ref, out, or in parameters.
    /// Methods with these parameters cannot be async, so assertions must use .Wait() instead of await.
    /// </summary>
    private bool _currentMethodHasRefOutInParameters;

    protected AssertionRewriter(SemanticModel semanticModel)
    {
        SemanticModel = semanticModel;
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Track whether this method has ref/out/in parameters
        var previousValue = _currentMethodHasRefOutInParameters;
        _currentMethodHasRefOutInParameters = node.ParameterList.Parameters.Any(p =>
            p.Modifiers.Any(SyntaxKind.RefKeyword) ||
            p.Modifiers.Any(SyntaxKind.OutKeyword) ||
            p.Modifiers.Any(SyntaxKind.InKeyword));

        try
        {
            return base.VisitMethodDeclaration(node);
        }
        finally
        {
            _currentMethodHasRefOutInParameters = previousValue;
        }
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

        // Wrap in await or .Wait() depending on whether the method can be async
        return WrapAssertionForAsync(fullInvocation);
    }

    /// <summary>
    /// Creates a TUnit generic assertion like Assert.That(value).IsTypeOf&lt;T&gt;()
    /// </summary>
    protected ExpressionSyntax CreateTUnitGenericAssertion(
        string methodName,
        ExpressionSyntax actualValue,
        TypeSyntax typeArg,
        ExpressionSyntax? message)
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

        // Create Assert.That(actualValue).MethodName<T>()
        var genericMethodName = SyntaxFactory.GenericName(methodName)
            .WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(typeArg)
                )
            );

        var methodAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            assertThatInvocation,
            genericMethodName
        );

        ExpressionSyntax fullInvocation = SyntaxFactory.InvocationExpression(methodAccess, SyntaxFactory.ArgumentList());

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

        // Wrap in await or .Wait() depending on whether the method can be async
        return WrapAssertionForAsync(fullInvocation);
    }

    /// <summary>
    /// Wraps an assertion expression in await or .Wait() depending on whether the containing method
    /// can be async (methods with ref/out/in parameters cannot be async).
    /// </summary>
    protected ExpressionSyntax WrapAssertionForAsync(ExpressionSyntax assertionExpression)
    {
        if (_currentMethodHasRefOutInParameters)
        {
            // Method has ref/out/in parameters, cannot be async - use .Wait()
            var waitAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                assertionExpression,
                SyntaxFactory.IdentifierName("Wait")
            );
            return SyntaxFactory.InvocationExpression(waitAccess, SyntaxFactory.ArgumentList());
        }

        // Method can be async - use await
        var awaitKeyword = SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);
        return SyntaxFactory.AwaitExpression(awaitKeyword, assertionExpression);
    }

    protected static bool IsEmptyOrNullMessage(ExpressionSyntax message)
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
    /// Uses semantic analysis when available, with syntax-based fallback for resilience across TFMs.
    /// </summary>
    protected bool IsLikelyComparerArgument(ArgumentSyntax argument)
    {
        // First, try syntax-based detection for string literals (most common message case)
        // This is deterministic and consistent across all TFMs
        if (argument.Expression.IsKind(SyntaxKind.StringLiteralExpression) ||
            argument.Expression.IsKind(SyntaxKind.InterpolatedStringExpression))
        {
            return false; // String literals are messages, not comparers
        }

        // Try semantic analysis
        var typeInfo = SemanticModel.GetTypeInfo(argument.Expression);
        if (typeInfo.Type != null && typeInfo.Type.TypeKind != TypeKind.Error)
        {
            var typeName = typeInfo.Type.ToDisplayString();

            // If it's a string type, it's definitely a message, not a comparer
            if (typeInfo.Type.SpecialType == SpecialType.System_String ||
                typeName == "string" || typeName == "System.String")
            {
                return false;
            }

            // Check for IComparer, IComparer<T>, IEqualityComparer, IEqualityComparer<T>
            if (typeName.Contains("IComparer") || typeName.Contains("IEqualityComparer"))
            {
                return true;
            }

            // Check interfaces - also check for generic interface names like IComparer`1
            if (typeInfo.Type is INamedTypeSymbol namedType)
            {
                if (namedType.AllInterfaces.Any(i =>
                    i.Name.StartsWith("IComparer") ||
                    i.Name.StartsWith("IEqualityComparer")))
                {
                    return true;
                }
            }

            // Also check if the type name itself contains Comparer (for StringComparer, etc.)
            if (typeName.Contains("Comparer"))
            {
                return true;
            }

            // Semantic analysis resolved to a non-comparer type
            return false;
        }

        // Fallback: Syntax-based detection when semantic analysis fails
        // This ensures consistent behavior across TFMs
        return IsLikelyComparerArgumentBySyntax(argument.Expression);
    }

    /// <summary>
    /// Syntax-based fallback for comparer detection. Used when semantic analysis fails.
    /// Must be deterministic to ensure consistent behavior across TFMs.
    /// </summary>
    private static bool IsLikelyComparerArgumentBySyntax(ExpressionSyntax expression)
    {
        var expressionText = expression.ToString();
        var lowerExpressionText = expressionText.ToLowerInvariant();

        // Check for variable names or expressions containing "comparer" (case-insensitive)
        // This catches variable names like 'comparer', 'myComparer', 'stringComparer', etc.
        if (lowerExpressionText.EndsWith("comparer") ||
            lowerExpressionText.Contains("comparer.") ||
            lowerExpressionText.Contains("comparer<") ||
            lowerExpressionText.Contains("equalitycomparer"))
        {
            return true;
        }

        // Check for new SomeComparer() or new SomeComparer<T>() patterns
        if (expression is ObjectCreationExpressionSyntax objectCreation)
        {
            var typeText = objectCreation.Type.ToString().ToLowerInvariant();
            if (typeText.Contains("comparer"))
            {
                return true;
            }
        }

        // Check for ImplicitObjectCreationExpressionSyntax (new() { ... })
        // These are ambiguous without semantic info, assume not a comparer
        if (expression is ImplicitObjectCreationExpressionSyntax)
        {
            return false;
        }

        // Default: assume it's a message (conservative - avoids incorrect comparer handling)
        return false;
    }

    /// <summary>
    /// Creates a TODO comment for unsupported features during migration.
    /// </summary>
    protected static SyntaxTrivia CreateTodoComment(string message)
    {
        return SyntaxFactory.Comment($"// TODO: TUnit migration - {message}");
    }
    
    /// <summary>
    /// Determines if an invocation is a framework assertion method.
    /// Uses semantic analysis when available, with syntax-based fallback for resilience across TFMs.
    /// </summary>
    protected bool IsFrameworkAssertion(InvocationExpressionSyntax invocation)
    {
        // Try semantic analysis first
        var symbolInfo = SemanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
        {
            var namespaceName = methodSymbol.ContainingNamespace?.ToDisplayString() ?? "";
            if (IsFrameworkAssertionNamespace(namespaceName))
            {
                return true;
            }
        }

        // Fallback: Syntax-based detection when semantic analysis fails
        // This ensures consistent behavior across TFMs
        return IsFrameworkAssertionBySyntax(invocation);
    }

    /// <summary>
    /// Syntax-based fallback for framework assertion detection. Used when semantic analysis fails.
    /// Must be deterministic to ensure consistent behavior across TFMs.
    /// </summary>
    private bool IsFrameworkAssertionBySyntax(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        var targetType = memberAccess.Expression.ToString();
        var methodName = memberAccess.Name.Identifier.Text;

        return IsKnownAssertionTypeBySyntax(targetType, methodName);
    }

    /// <summary>
    /// Checks if the target type and method name match known framework assertion patterns.
    /// Override in derived classes to provide framework-specific patterns.
    /// </summary>
    protected abstract bool IsKnownAssertionTypeBySyntax(string targetType, string methodName);

    protected abstract bool IsFrameworkAssertionNamespace(string namespaceName);
}