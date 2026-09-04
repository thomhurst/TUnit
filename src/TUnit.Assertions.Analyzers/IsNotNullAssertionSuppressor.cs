using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Assertions.Analyzers.Extensions;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Suppresses nullability warnings (CS8600, CS8602, CS8604, CS8618, CS8629) for variables
/// after they have been asserted as non-null using Assert.That(x).IsNotNull()
/// or x.Should().NotBeNull().
///
/// Note: This suppressor only hides the warnings; it does not change the compiler's
/// null-state flow analysis. Variables will still appear as nullable in IntelliSense.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class IsNotNullAssertionSuppressor : DiagnosticSuppressor
{
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            // Only process nullability warnings
            if (!IsNullabilityWarning(diagnostic.Id))
            {
                continue;
            }

            // Get the syntax tree and semantic model
            if (diagnostic.Location.SourceTree is not { } sourceTree)
            {
                continue;
            }

            var root = sourceTree.GetRoot();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            if (node is null)
            {
                continue;
            }

            var semanticModel = context.GetSemanticModel(sourceTree);

            // Find the variable/expression being referenced that caused the warning
            var targetExpression = GetTargetExpression(node);
            if (targetExpression is null)
            {
                continue;
            }

            // Check if this variable/expression was previously asserted as non-null
            if (WasAssertedNotNull(targetExpression, semanticModel, context.CancellationToken))
            {
                Suppress(context, diagnostic);
            }
        }
    }

    private bool IsNullabilityWarning(string diagnosticId)
    {
        return diagnosticId is "CS8600" // Converting null literal or possible null value to non-nullable type
            or "CS8602" // Dereference of a possibly null reference
            or "CS8604" // Possible null reference argument
            or "CS8618" // Non-nullable field/property uninitialized
            or "CS8629"; // Nullable value type may be null
    }

    private ExpressionSyntax? GetTargetExpression(SyntaxNode node)
    {
        // The warning might be on the identifier itself, a member access, or a parent node
        return node switch
        {
            IdentifierNameSyntax identifier => identifier,
            MemberAccessExpressionSyntax memberAccess => memberAccess,
            ArgumentSyntax { Expression: var expression } => expression,
            _ => node.DescendantNodesAndSelf()
                .OfType<ExpressionSyntax>()
                .FirstOrDefault(e => e is IdentifierNameSyntax or MemberAccessExpressionSyntax)
        };
    }

    // Statement-order match only — not control-flow aware. An assertion inside an `if (cond)` or
    // `try`/`catch` branch suppresses warnings on subsequent uses even when the assertion may not
    // have run on every path. Accepting that imprecision keeps the analyzer cheap; the alternative
    // (full dataflow analysis via Roslyn's IFlowAnalysis) is significant complexity for a niche
    // false-suppression case. See AwaitAssertionAnalyzer for the symmetric awaitedness check.
    private bool WasAssertedNotNull(
        ExpressionSyntax targetExpression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Find the innermost containing scope (lambda, local function, or method)
        SyntaxNode? containingMethod = null;
        foreach (var ancestor in targetExpression.Ancestors())
        {
            if (ancestor is MethodDeclarationSyntax
                or LocalFunctionStatementSyntax
                or AnonymousFunctionExpressionSyntax)
            {
                containingMethod = ancestor;
                break;
            }
        }

        if (containingMethod is null)
        {
            return false;
        }

        // Look for Assert.That(variable).IsNotNull() patterns before this usage
        var allStatements = containingMethod.DescendantNodes().OfType<StatementSyntax>().ToList();
        var identifierStatement = targetExpression.FirstAncestorOrSelf<StatementSyntax>();

        if (identifierStatement is null)
        {
            return false;
        }

        var identifierStatementIndex = allStatements.IndexOf(identifierStatement);
        if (identifierStatementIndex < 0)
        {
            return false;
        }

        // Check all statements before the current one
        for (int i = 0; i < identifierStatementIndex; i++)
        {
            var statement = allStatements[i];

            // Look for await Assert.That(x).IsNotNull() pattern
            if (IsNotNullAssertion(statement, targetExpression, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsNotNullAssertion(
        StatementSyntax statement,
        ExpressionSyntax targetExpression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Patterns recognised:
        //   await Assert.That(variable).IsNotNull()
        //   await Assert.That(variable).Contains("test").And.IsNotNull()
        //   Assert.That(variable).IsNotNull().GetAwaiter().GetResult()
        //   await variable.Should().NotBeNull()
        //   await variable.Should().Contain("test").And.NotBeNull()

        var invocations = statement.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: var calledName })
            {
                continue;
            }

            ExpressionSyntax? targetArgument = calledName switch
            {
                "IsNotNull" => GetAssertThatArgument(invocation, semanticModel, cancellationToken),
                "NotBeNull" => GetShouldReceiver(invocation, semanticModel, cancellationToken),
                _ => null,
            };

            if (targetArgument is not null
                && ExpressionsMatch(targetArgument, targetExpression, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static ExpressionSyntax? GetAssertThatArgument(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var assertThatCall = FindAssertThatInChain(invocation);
        if (assertThatCall is null
            || assertThatCall.ArgumentList.Arguments.Count != 1
            || !IsSupportedAssertionChain(invocation, assertThatCall, semanticModel, cancellationToken)
            || !IsTUnitIsNotNullMethod(invocation, semanticModel, cancellationToken)
            || !IsTUnitMethod(
                assertThatCall,
                semanticModel,
                cancellationToken,
                "global::TUnit.Assertions.Assert",
                "That"))
        {
            return null;
        }

        return assertThatCall.ArgumentList.Arguments[0].Expression;
    }

    private static bool IsTUnitIsNotNullMethod(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol symbol)
        {
            return false;
        }

        var method = symbol.ReducedFrom ?? symbol;

        if (method.Name != "IsNotNull")
        {
            return false;
        }

        if (method.ContainingType.GloballyQualifiedNonGeneric() == "global::TUnit.Assertions.Extensions.AssertionExtensions")
        {
            return true;
        }

        var collectionBase = semanticModel.Compilation.GetTypeByMetadataName(
            "TUnit.Assertions.Sources.CollectionAssertionBase`2");

        // Check the declaring assembly as well as the shared base: a custom subclass
        // can hide IsNotNull, but that does not make its method a TUnit null check.
        if (collectionBase is null
            || !SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, collectionBase.ContainingAssembly))
        {
            return false;
        }

        for (var type = method.ContainingType; type is not null; type = type.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, collectionBase))
            {
                return true;
            }
        }

        return false;
    }

    private static ExpressionSyntax? GetShouldReceiver(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (!IsTUnitMethod(
                invocation,
                semanticModel,
                cancellationToken,
                "global::TUnit.Assertions.Should.Extensions.ShouldAssertionExtensions",
                "NotBeNull"))
        {
            return null;
        }

        var shouldCall = FindShouldInChain(invocation);
        if (shouldCall is null
            || !IsSupportedAssertionChain(invocation, shouldCall, semanticModel, cancellationToken)
            || !IsTUnitMethod(
                shouldCall,
                semanticModel,
                cancellationToken,
                "global::TUnit.Assertions.Should.ShouldExtensions",
                "Should"))
        {
            return null;
        }

        // Should is an extension method — its receiver is the value being asserted.
        return shouldCall.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Expression
            : null;
    }

    private static bool IsSupportedAssertionChain(
        InvocationExpressionSyntax nullCheck,
        InvocationExpressionSyntax entryPoint,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        ExpressionSyntax outermost = nullCheck;
        while ((outermost.Parent is MemberAccessExpressionSyntax member && member.Expression == outermost)
               || (outermost.Parent is InvocationExpressionSyntax call && call.Expression == outermost)
               || outermost.Parent is ParenthesizedExpressionSyntax)
        {
            outermost = (ExpressionSyntax)outermost.Parent;
        }

        // Or on either side makes the null check optional. Walk only the receiver
        // chain, not nested arguments or lambdas belonging to another assertion.
        for (ExpressionSyntax? current = outermost;
             current is not null && current != entryPoint;
             current = GetChainReceiver(current))
        {
            if (current is MemberAccessExpressionSyntax { Name.Identifier.Text: "Or" })
            {
                return false;
            }
        }

        var assertionAssembly = semanticModel.Compilation.GetTypeByMetadataName("TUnit.Assertions.Assert")?.ContainingAssembly;
        var entryAssembly = semanticModel.GetSymbolInfo(entryPoint, cancellationToken).Symbol?.ContainingAssembly;

        // An external method/property can return a TUnit assertion on another value.
        // Do not trace a null check back across such a transformation.
        for (ExpressionSyntax? current = nullCheck; current is not null; current = GetChainReceiver(current))
        {
            if (current == entryPoint)
            {
                return true;
            }

            if (current is not ParenthesizedExpressionSyntax)
            {
                var symbol = semanticModel.GetSymbolInfo(current, cancellationToken).Symbol;
                if (symbol is null
                    || !(SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, assertionAssembly)
                         || SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, entryAssembly)))
                {
                    return false;
                }
            }
        }

        return false;
    }

    private static ExpressionSyntax? GetChainReceiver(ExpressionSyntax expression) => expression switch
    {
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } => member.Expression,
        MemberAccessExpressionSyntax member => member.Expression,
        ParenthesizedExpressionSyntax parenthesized => parenthesized.Expression,
        _ => null,
    };

    private static bool IsTUnitMethod(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        string fullyQualifiedContainingTypeName,
        string methodName)
    {
        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol symbol)
        {
            return false;
        }

        var method = symbol.ReducedFrom ?? symbol;
        return method.Name == methodName
               && method.ContainingType.GloballyQualifiedNonGeneric() == fullyQualifiedContainingTypeName;
    }

    private bool ExpressionsMatch(
        ExpressionSyntax assertArgument,
        ExpressionSyntax targetExpression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // For simple identifiers, compare using semantic symbols (handles renames, etc.)
        if (assertArgument is IdentifierNameSyntax && targetExpression is IdentifierNameSyntax)
        {
            return SymbolsMatch(assertArgument, targetExpression, semanticModel, cancellationToken);
        }

        // For member access chains (e.g., value.Id), recursively compare member and receiver
        if (assertArgument is MemberAccessExpressionSyntax assertMember &&
            targetExpression is MemberAccessExpressionSyntax targetMember)
        {
            return SymbolsMatch(assertMember, targetMember, semanticModel, cancellationToken) &&
                   ExpressionsMatch(assertMember.Expression, targetMember.Expression, semanticModel, cancellationToken);
        }

        // Mismatched expression types (e.g., identifier vs member access) are intentionally
        // not matched — asserting `id` should not suppress warnings on `wrapper.Id` or vice versa.
        return false;
    }

    private bool SymbolsMatch(
        ExpressionSyntax expr1,
        ExpressionSyntax expr2,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var symbol1 = semanticModel.GetSymbolInfo(expr1, cancellationToken).Symbol;
        var symbol2 = semanticModel.GetSymbolInfo(expr2, cancellationToken).Symbol;
        return symbol1 is not null && SymbolEqualityComparer.Default.Equals(symbol1, symbol2);
    }

    private static InvocationExpressionSyntax? FindAssertThatInChain(InvocationExpressionSyntax invocation)
        => FindInvocationInChain(invocation, identifierName: "That", parentName: "Assert");

    // Should() is an extension method, so its receiver is the asserted value (any expression).
    // parentName MUST stay null because the receiver is the asserted value. Semantic validation
    // in GetShouldReceiver still ensures that only TUnit's Should extension qualifies.
    private static InvocationExpressionSyntax? FindShouldInChain(InvocationExpressionSyntax invocation)
        => FindInvocationInChain(invocation, identifierName: "Should", parentName: null);

    /// <summary>
    /// Walks up an expression chain looking for an invocation whose member-access name is
    /// <paramref name="identifierName"/>. When <paramref name="parentName"/> is non-null the
    /// invocation must also be of the form <c>{parentName}.{identifierName}(...)</c>; for
    /// extension methods (<c>Should</c>) the receiver is arbitrary so parentName is null.
    /// </summary>
    private static InvocationExpressionSyntax? FindInvocationInChain(
        InvocationExpressionSyntax invocation,
        string identifierName,
        string? parentName)
    {
        var current = invocation.Expression;

        while (current is not null)
        {
            if (current is InvocationExpressionSyntax invocationExpr)
            {
                if (invocationExpr.Expression is MemberAccessExpressionSyntax memberExpr
                    && memberExpr.Name.Identifier.Text == identifierName
                    && (parentName is null
                        || (memberExpr.Expression is IdentifierNameSyntax id && id.Identifier.Text == parentName)))
                {
                    return invocationExpr;
                }

                current = invocationExpr.Expression;
            }
            else if (current is MemberAccessExpressionSyntax memberAccess)
            {
                current = memberAccess.Expression;
            }
            else
            {
                break;
            }
        }

        return null;
    }

    private void Suppress(SuppressionAnalysisContext context, Diagnostic diagnostic)
    {
        if (SuppressionsByDiagnosticId.TryGetValue(diagnostic.Id, out var suppression))
        {
            context.ReportSuppression(
                Suppression.Create(
                    suppression,
                    diagnostic
                )
            );
        }
    }

    private static readonly SuppressionDescriptor[] Descriptors =
    [
        CreateDescriptor("CS8600"),
        CreateDescriptor("CS8602"),
        CreateDescriptor("CS8604"),
        CreateDescriptor("CS8618"),
        CreateDescriptor("CS8629"),
    ];

    private static readonly Dictionary<string, SuppressionDescriptor> SuppressionsByDiagnosticId =
        Descriptors.ToDictionary(d => d.SuppressedDiagnosticId);

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
        ImmutableArray.Create(Descriptors);

    private static SuppressionDescriptor CreateDescriptor(string id)
        => new(
            id: $"{id}Suppression",
            suppressedDiagnosticId: id,
            justification: $"Suppress {id} for variables asserted as non-null via Assert.That(x).IsNotNull() or x.Should().NotBeNull()."
        );
}
