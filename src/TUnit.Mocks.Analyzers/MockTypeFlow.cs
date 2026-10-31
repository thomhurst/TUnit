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
    /// doesn't depend on the expression's own type, so its type can change from <c>Mock&lt;T&gt;</c>
    /// to <paramref name="newType"/> (the generated wrapper) without changing what the code means.
    /// Anything not recognised counts as sensitive. When <paramref name="newType"/> is unknown,
    /// positions that need it (arguments, casts, type tests, equality) count as sensitive too.
    /// </summary>
    internal static bool IsTypeInsensitivePosition(
        SyntaxNode node,
        ITypeSymbol? newType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        switch (node.Parent)
        {
            // mock.Member / mock?.Member: the wrapper derives from Mock<T> and implements T
            // explicitly, so the same members bind.
            case MemberAccessExpressionSyntax memberAccess:
                return memberAccess.Expression == node;
            case ConditionalAccessExpressionSyntax conditionalAccess:
                return conditionalAccess.Expression == node;

            case ArgumentSyntax argument when argument.RefKindKeyword.IsKind(SyntaxKind.None):
                return newType is not null
                       && argument.Parent?.Parent is { } outer
                       && IsOverloadResolutionUnchanged(argument, outer, newType, semanticModel, cancellationToken);

            // Explicitly typed targets.
            case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: false } } }:
            case EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax or ParameterSyntax }:
                return true;
            case AssignmentExpressionSyntax assignment:
                return assignment.Right == node;

            // The wrapper is sealed: a cast or type test to an interface it doesn't implement is
            // legal on Mock<T> but an error (or an always-false warning) on the wrapper.
            case CastExpressionSyntax cast:
                return IsConvertibleTo(newType, cast.Type, semanticModel, cancellationToken);
            case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AsExpression):
                return binary.Left == node
                       && binary.Right is TypeSyntax asType
                       && IsConvertibleTo(newType, asType, semanticModel, cancellationToken);
            // `wrapper is IFoo` is always true for a non-null wrapper and warns (CS0183), so the
            // tested type must still need a runtime check.
            case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.IsExpression):
                return binary.Left == node
                       && binary.Right is TypeSyntax testedType
                       && IsConvertibleTo(newType, testedType, semanticModel, cancellationToken, allowImplicit: false);
            case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression):
                return IsComparableWith(newType, binary.Left == node ? binary.Right : binary.Left, semanticModel, cancellationToken);
            case BinaryExpressionSyntax:
                return true;
            case IsPatternExpressionSyntax isPattern:
                return isPattern.Expression == node
                       && IsPatternStillApplicable(isPattern.Pattern, newType, semanticModel, cancellationToken);

            case InterpolationSyntax:
            case ExpressionStatementSyntax:
            case ExpressionElementSyntax:
            case YieldStatementSyntax:
                return true;
            case InitializerExpressionSyntax initializer when initializer.IsKind(SyntaxKind.CollectionInitializerExpression):
                return HasSingleAddOverload(node, semanticModel, cancellationToken);
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

    /// <summary>
    /// Rebinds the call (or constructor, or indexer) with the argument given the new type and
    /// checks it still picks the same member. This catches overloads such as
    /// <c>Use(Mock&lt;IFoo&gt;)</c> + <c>Use(IFoo)</c> becoming ambiguous, or an overload taking the
    /// new type being picked instead.
    /// </summary>
    private static bool IsOverloadResolutionUnchanged(
        ArgumentSyntax argument,
        SyntaxNode outer,
        ITypeSymbol newType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (outer is not (InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax or ElementAccessExpressionSyntax
                or ConstructorInitializerSyntax or PrimaryConstructorBaseTypeSyntax)
            || semanticModel.GetSymbolInfo(outer, cancellationToken).Symbol is not { } original)
        {
            return false;
        }

        // Generic type inference would pick up the new type even when the same method is chosen.
        if (original is IMethodSymbol { IsGenericMethod: true }
            && outer is InvocationExpressionSyntax invocation
            && GetInvokedName(invocation) is not GenericNameSyntax)
        {
            return false;
        }

        var retyped = SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.CastExpression(
                SyntaxFactory.ParseTypeName(newType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                SyntaxFactory.ParenthesizedExpression(argument.Expression.WithoutTrivia())));

        var speculative = outer.ReplaceNode(argument, argument.WithExpression(retyped));
        var position = outer.SpanStart;

        var rebound = speculative switch
        {
            ConstructorInitializerSyntax constructorInitializer => semanticModel.GetSpeculativeSymbolInfo(position, constructorInitializer).Symbol,
            PrimaryConstructorBaseTypeSyntax primaryConstructorBase => semanticModel.GetSpeculativeSymbolInfo(position, primaryConstructorBase).Symbol,
            ExpressionSyntax expression => semanticModel.GetSpeculativeSymbolInfo(position, expression, SpeculativeBindingOption.BindAsExpression).Symbol,
            _ => null,
        };

        return SymbolEqualityComparer.Default.Equals(original, rebound);
    }

    private static bool IsConvertibleTo(
        ITypeSymbol? newType,
        TypeSyntax targetSyntax,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        bool allowImplicit = true)
    {
        if (newType is null || semanticModel.GetTypeInfo(targetSyntax, cancellationToken).Type is not { } target)
        {
            return false;
        }

        var conversion = semanticModel.Compilation.ClassifyCommonConversion(newType, target);
        return conversion.Exists && (allowImplicit || !conversion.IsImplicit);
    }

    private static bool IsComparableWith(ITypeSymbol? newType, ExpressionSyntax other, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (other.IsKind(SyntaxKind.NullLiteralExpression))
        {
            return true;
        }

        if (newType is null || semanticModel.GetTypeInfo(other, cancellationToken).Type is not { } otherType)
        {
            return false;
        }

        var compilation = semanticModel.Compilation;
        return compilation.ClassifyCommonConversion(newType, otherType).Exists
               || compilation.ClassifyCommonConversion(otherType, newType).Exists;
    }

    private static bool IsPatternStillApplicable(PatternSyntax pattern, ITypeSymbol? newType, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        return pattern switch
        {
            ConstantPatternSyntax => true,
            ParenthesizedPatternSyntax parenthesized => IsPatternStillApplicable(parenthesized.Pattern, newType, semanticModel, cancellationToken),
            UnaryPatternSyntax unary => IsPatternStillApplicable(unary.Pattern, newType, semanticModel, cancellationToken),
            BinaryPatternSyntax binary => IsPatternStillApplicable(binary.Left, newType, semanticModel, cancellationToken)
                                          && IsPatternStillApplicable(binary.Right, newType, semanticModel, cancellationToken),
            DeclarationPatternSyntax declaration => IsConvertibleTo(newType, declaration.Type, semanticModel, cancellationToken),
            TypePatternSyntax typePattern => IsConvertibleTo(newType, typePattern.Type, semanticModel, cancellationToken),
            RecursivePatternSyntax { Type: { } type } => IsConvertibleTo(newType, type, semanticModel, cancellationToken),
            RecursivePatternSyntax => true,
            _ => false,
        };
    }

    /// <summary>
    /// A collection initializer element is passed to an <c>Add</c> method. With more than one
    /// overload, the new type could pick a different one.
    /// </summary>
    private static bool HasSingleAddOverload(SyntaxNode element, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (element is not ExpressionSyntax expression
            || semanticModel.GetCollectionInitializerSymbolInfo(expression, cancellationToken).Symbol
                is not IMethodSymbol { IsGenericMethod: false, ReducedFrom: null } add)
        {
            return false;
        }

        var overloads = 0;
        for (var type = add.ContainingType; type is not null; type = type.BaseType)
        {
            foreach (var member in type.GetMembers(add.Name))
            {
                if (member is IMethodSymbol && ++overloads > 1)
                {
                    return false;
                }
            }
        }

        return true;
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
