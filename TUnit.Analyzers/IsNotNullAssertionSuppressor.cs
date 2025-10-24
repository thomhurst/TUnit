using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

/// <summary>
/// Suppresses nullability warnings (CS8600, CS8602, CS8604, CS8618) for variables
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

            // Find the variable being referenced that caused the warning
            var identifierName = GetIdentifierFromNode(node);
            if (identifierName is null)
            {
                continue;
            }

            // Check if this variable was previously asserted as non-null
            if (WasAssertedNotNull(identifierName, semanticModel, context.CancellationToken))
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
            or "CS8618"; // Non-nullable field/property uninitialized
    }

    private IdentifierNameSyntax? GetIdentifierFromNode(SyntaxNode node)
    {
        // The warning might be on the identifier itself or a parent node
        return node switch
        {
            IdentifierNameSyntax identifier => identifier,
            MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax identifier } => identifier,
            ArgumentSyntax { Expression: IdentifierNameSyntax identifier } => identifier,
            _ => node.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().FirstOrDefault()
        };
    }

    private bool WasAssertedNotNull(
        IdentifierNameSyntax identifierName,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol;
        if (symbol is null)
        {
            return false;
        }

        // Find the containing method/block
        var containingMethod = identifierName.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (containingMethod is null)
        {
            return false;
        }

        // Look for Assert.That(variable).IsNotNull() patterns before this usage
        var allStatements = containingMethod.DescendantNodes().OfType<StatementSyntax>().ToList();
        var identifierStatement = identifierName.FirstAncestorOrSelf<StatementSyntax>();

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
            if (IsNotNullAssertion(statement, symbol, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsNotNullAssertion(
        StatementSyntax statement,
        ISymbol targetSymbol,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Pattern: await Assert.That(variable).IsNotNull()
        // or: Assert.That(variable).IsNotNull().GetAwaiter().GetResult()

        var invocations = statement.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            // Check if this is a call to IsNotNull()
            if (invocation.Expression is not MemberAccessExpressionSyntax
                {
                    Name.Identifier.Text: "IsNotNull",
                    Expression: InvocationExpressionSyntax assertThatCall
                })
            {
                continue;
            }

            // Check if the Assert.That call is being made
            if (assertThatCall.Expression is not MemberAccessExpressionSyntax
                {
                    Name.Identifier.Text: "That",
                    Expression: IdentifierNameSyntax { Identifier.Text: "Assert" }
                })
            {
                continue;
            }

            // Get the argument to Assert.That()
            if (assertThatCall.ArgumentList.Arguments.Count != 1)
            {
                continue;
            }

            var argument = assertThatCall.ArgumentList.Arguments[0].Expression;

            // Get the symbol of the argument
            var argumentSymbol = semanticModel.GetSymbolInfo(argument, cancellationToken).Symbol;

            // Check if it's the same symbol we're looking for
            if (SymbolEqualityComparer.Default.Equals(argumentSymbol, targetSymbol))
            {
                return true;
            }
        }

        return false;
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
            CreateDescriptor("CS8618")
        );

    private static SuppressionDescriptor CreateDescriptor(string id)
        => new(
            id: $"{id}Suppression",
            suppressedDiagnosticId: id,
            justification: $"Suppress {id} for variables asserted as non-null via Assert.That(x).IsNotNull()."
        );
}
