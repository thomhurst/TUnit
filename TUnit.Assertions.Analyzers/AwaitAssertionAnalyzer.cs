using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Shared;
using TUnit.Analyzers.Shared.Extensions;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// A sample analyzer that reports the company name being used in class declarations.
/// Traverses through the Syntax Tree and checks the name (identifier) of each class node.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AwaitAssertionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.AwaitAssertion);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SimpleMemberAccessExpression);
    }

    /// <summary>
    /// Executed for each Syntax Node with 'SyntaxKind' is 'ClassDeclaration'.
    /// </summary>
    /// <param name="context">Operation context.</param>
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        // The Roslyn architecture is based on inheritance.
        // To get the required metadata, we should match the 'Node' object to the particular type: 'ClassDeclarationSyntax'.
        if (context.Node is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            return;
        }

        if (memberAccessExpressionSyntax.ToString() is not "Assert.That" and not "Assert.Multiple")
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(memberAccessExpressionSyntax);

        if (symbol.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (methodSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) 
            is not "global::TUnit.Assertions.Assert.That"
            and not "global::TUnit.Assertions.Assert.Multiple")
        {
            return;
        }

        if (memberAccessExpressionSyntax.ToString() is "Assert.That"
            && memberAccessExpressionSyntax.GetAllAncestorSyntaxesOfType<InvocationExpressionSyntax>()
                .Any(x => x.ToString().StartsWith("Assert.Multiple")))
        {
            return;
        }

        var expressionStatementParent = memberAccessExpressionSyntax.GetAncestorSyntaxOfType<ExpressionStatementSyntax>();

        if (expressionStatementParent is null)
        {
            return;
        }
        
        if (expressionStatementParent.ChildNodes().Any(x => x is AwaitExpressionSyntax))
        {
            return;
        }
        
        context.ReportDiagnostic(
            Diagnostic.Create(Rules.AwaitAssertion, expressionStatementParent.GetLocation())
        );
    }
}