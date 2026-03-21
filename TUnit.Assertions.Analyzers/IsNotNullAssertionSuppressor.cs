using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
            _ => node.DescendantNodesAndSelf().OfType<ExpressionSyntax>().FirstOrDefault()
        };
    }

    private bool WasAssertedNotNull(
        ExpressionSyntax targetExpression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Find the containing method/block
        var containingMethod = targetExpression.FirstAncestorOrSelf<MethodDeclarationSyntax>();
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
        // Pattern: await Assert.That(variable).IsNotNull()
        // or: await Assert.That(variable).Contains("test").And.IsNotNull()
        // or: Assert.That(variable).IsNotNull().GetAwaiter().GetResult()

        var invocations = statement.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            // Check if this is a call to IsNotNull()
            if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "IsNotNull" })
            {
                continue;
            }

            // Walk up the expression chain to find Assert.That() call
            var assertThatCall = FindAssertThatInChain(invocation);
            if (assertThatCall is null)
            {
                continue;
            }

            // Get the argument to Assert.That()
            if (assertThatCall.ArgumentList.Arguments.Count != 1)
            {
                continue;
            }

            var argument = assertThatCall.ArgumentList.Arguments[0].Expression;

            // Check if the argument matches the target expression
            if (ExpressionsMatch(argument, targetExpression, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
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
            var argumentSymbol = semanticModel.GetSymbolInfo(assertArgument, cancellationToken).Symbol;
            var targetSymbol = semanticModel.GetSymbolInfo(targetExpression, cancellationToken).Symbol;
            return argumentSymbol is not null && SymbolEqualityComparer.Default.Equals(argumentSymbol, targetSymbol);
        }

        // For complex expressions (member access chains like value.Id), compare structurally
        return assertArgument.IsEquivalentTo(targetExpression);
    }

    private InvocationExpressionSyntax? FindAssertThatInChain(InvocationExpressionSyntax invocation)
    {
        // Walk up the expression chain looking for Assert.That()
        var current = invocation.Expression;

        while (current is not null)
        {
            if (current is InvocationExpressionSyntax invocationExpr)
            {
                // Check if this is Assert.That()
                if (invocationExpr.Expression is MemberAccessExpressionSyntax
                    {
                        Name.Identifier.Text: "That",
                        Expression: IdentifierNameSyntax { Identifier.Text: "Assert" }
                    })
                {
                    return invocationExpr;
                }

                // Continue walking up from this invocation
                current = invocationExpr.Expression;
            }
            else if (current is MemberAccessExpressionSyntax memberAccess)
            {
                // Move to the expression being accessed
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
        var suppression = SupportedSuppressions.FirstOrDefault(s => s.SuppressedDiagnosticId == diagnostic.Id);

        if (suppression is not null)
        {
            context.ReportSuppression(
                Suppression.Create(
                    suppression,
                    diagnostic
                )
            );
        }
    }

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
        ImmutableArray.Create(
            CreateDescriptor("CS8600"),
            CreateDescriptor("CS8602"),
            CreateDescriptor("CS8604"),
            CreateDescriptor("CS8618"),
            CreateDescriptor("CS8629")
        );

    private static SuppressionDescriptor CreateDescriptor(string id)
        => new(
            id: $"{id}Suppression",
            suppressedDiagnosticId: id,
            justification: $"Suppress {id} for variables asserted as non-null via Assert.That(x).IsNotNull()."
        );
}
