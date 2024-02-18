using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// A sample analyzer that reports the company name being used in class declarations.
/// Traverses through the Syntax Tree and checks the name (identifier) of each class node.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StringEqualsUseComparerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.StringEqualsUseComparer);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.IdentifierName);

        // Check other 'context.Register...' methods that might be helpful for your purposes.
    }

    /// <summary>
    /// Executed for each Syntax Node with 'SyntaxKind' is 'ClassDeclaration'.
    /// </summary>
    /// <param name="context">Operation context.</param>
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        // The Roslyn architecture is based on inheritance.
        // To get the required metadata, we should match the 'Node' object to the particular type: 'ClassDeclarationSyntax'.
        if (context.Node is not IdentifierNameSyntax identifierNameSyntax)
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(identifierNameSyntax).Symbol;

        if (symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (methodSymbol.ToDisplayString() !=
            "TUnit.Assertions.Is<string, TUnit.Assertions.AssertConditions.Operators.ValueAnd<string>, TUnit.Assertions.AssertConditions.Operators.ValueOr<string>>.EqualTo<TUnit.Assertions.AssertConditions.Operators.ValueAnd<string>, TUnit.Assertions.AssertConditions.Operators.ValueOr<string>>(string, string)")
        {
            return;
        }

        if (methodSymbol.Parameters.Length == 2)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.StringEqualsUseComparer, context.Node.GetLocation())
            );
        }
    }
}