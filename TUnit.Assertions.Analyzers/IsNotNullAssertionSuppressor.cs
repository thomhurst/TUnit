using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Assertions.Analyzers.Extensions;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Suppresses nullability warnings (CS8600, CS8602, CS8604, CS8618, CS8629) for variables
/// after they have been asserted as non-null using Assert.That(x).IsNotNull().
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
            || !IsTUnitMethod(assertThatCall, semanticModel, cancellationToken, "global::TUnit.Assertions.Assert.That"))
        {
            return null;
        }

        return assertThatCall.ArgumentList.Arguments[0].Expression;
    }

    private static ExpressionSyntax? GetShouldReceiver(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var shouldCall = FindShouldInChain(invocation);
        if (shouldCall is null
            || !IsTUnitMethod(shouldCall, semanticModel, cancellationToken, "global::TUnit.Assertions.Should.ShouldExtensions.Should"))
        {
            return null;
        }

        // Should is an extension method — its receiver is the value being asserted.
        return shouldCall.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Expression
            : null;
    }

    private static bool IsTUnitMethod(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        string fullyQualifiedNonGenericName)
        => semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol symbol
           && symbol.GloballyQualifiedNonGeneric() == fullyQualifiedNonGenericName;

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
    // parentName MUST stay null — constraining it would break the suppressor for user-defined
    // assertion entry points and for Should() reached via using-aliases / namespace imports.
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
            justification: $"Suppress {id} for variables asserted as non-null via Assert.That(x).IsNotNull()."
        );
}
