using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Mocks.Analyzers;

/// <summary>
/// Shared checks for whether changing the static type of an expression (for example from
/// <c>Mock&lt;T&gt;</c> to the generated wrapper type that derives from it) could change how the
/// surrounding code binds.
/// </summary>
internal static class MockTypeFlow
{
    /// <summary>
    /// Climbs through expressions whose type flows from this operand (parentheses, conditional,
    /// null-coalescing and switch expressions).
    /// </summary>
    internal static SyntaxNode ClimbTypeFlow(SyntaxNode node)
    {
        while (node.Parent is { } parent)
        {
            if (parent is ParenthesizedExpressionSyntax
                || (parent is ConditionalExpressionSyntax conditional && node != conditional.Condition)
                || parent.IsKind(SyntaxKind.CoalesceExpression))
            {
                node = parent;
                continue;
            }

            if (parent is SwitchExpressionArmSyntax arm && node == arm.Expression && arm.Parent is { } switchExpression)
            {
                node = switchExpression;
                continue;
            }

            break;
        }

        return node;
    }

    /// <summary>
    /// Whether an expression in this position is converted to, or used through, a type that
    /// doesn't depend on the expression's own type. Anything not recognised counts as sensitive.
    /// </summary>
    internal static bool IsTypeInsensitivePosition(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        switch (node.Parent)
        {
            // mock.Member / mock?.Member: the wrapper derives from Mock<T>, so the same members bind.
            case MemberAccessExpressionSyntax memberAccess:
                return memberAccess.Expression == node;
            case ConditionalAccessExpressionSyntax conditionalAccess:
                return conditionalAccess.Expression == node;

            case ArgumentSyntax argument when argument.RefKindKeyword.IsKind(SyntaxKind.None):
                return argument.Parent?.Parent switch
                {
                    // Generic type inference would pick up the wrapper type.
                    InvocationExpressionSyntax outer =>
                        semanticModel.GetSymbolInfo(outer, cancellationToken).Symbol is IMethodSymbol method
                        && (!method.IsGenericMethod || GetInvokedName(outer) is GenericNameSyntax),
                    BaseObjectCreationExpressionSyntax or ConstructorInitializerSyntax or PrimaryConstructorBaseTypeSyntax or ElementAccessExpressionSyntax => true,
                    _ => false,
                };

            // Explicitly typed targets.
            case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: false } } }:
            case EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax or ParameterSyntax }:
                return true;
            case AssignmentExpressionSyntax assignment:
                return assignment.Right == node;
            case CastExpressionSyntax:
            case BinaryExpressionSyntax:
            case IsPatternExpressionSyntax:
            case InterpolationSyntax:
            case ExpressionStatementSyntax:
            case ExpressionElementSyntax:
            case YieldStatementSyntax:
                return true;
            case InitializerExpressionSyntax initializer:
                return initializer.Parent is not (ImplicitArrayCreationExpressionSyntax or AnonymousObjectCreationExpressionSyntax);

            // Declared return types, except for lambdas whose return type is inferred.
            case ReturnStatementSyntax returnStatement:
                return returnStatement.Ancestors().FirstOrDefault(static a =>
                    a is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or MemberDeclarationSyntax) is not AnonymousFunctionExpressionSyntax;
            case ArrowExpressionClauseSyntax:
                return true;

            default:
                return false;
        }
    }

    internal static SimpleNameSyntax? GetInvokedName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
            SimpleNameSyntax simpleName => simpleName,
            _ => null,
        };
    }

    internal static bool IsWrite(ExpressionSyntax reference)
    {
        SyntaxNode expression = reference;
        while (expression.Parent is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized;
        }

        return expression.Parent switch
        {
            AssignmentExpressionSyntax assignment => assignment.Left == expression,
            ArgumentSyntax argument => !argument.RefKindKeyword.IsKind(SyntaxKind.None) && !argument.RefKindKeyword.IsKind(SyntaxKind.InKeyword),
            RefExpressionSyntax => true,
            _ => false,
        };
    }
}
