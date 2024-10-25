using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// A sample analyzer that reports the company name being used in class declarations.
/// Traverses through the Syntax Tree and checks the name (identifier) of each class node.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CompilerArgumentsPopulatedAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.CompilerArgumentsPopulated);

    public override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.Argument);
    }

    /// <summary>
    /// Executed for each Syntax Node with 'SyntaxKind' is 'ClassDeclaration'.
    /// </summary>
    /// <param name="context">Operation context.</param>
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        // The Roslyn architecture is based on inheritance.
        // To get the required metadata, we should match the 'Node' object to the particular type: 'ClassDeclarationSyntax'.
        if (context.Node is not ArgumentSyntax argumentSyntax)
        {
            return;
        }

        var operation = context.SemanticModel.GetOperation(argumentSyntax);

        if (operation is not IArgumentOperation argumentOperation)
        {
            return;
        }

        if (argumentOperation.Parent?.Type?.ContainingAssembly?.Name is not "TUnit.Assertions")
        {
            return;
        }

        if (argumentOperation.Parameter?.GetAttributes().Any(x =>
                x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                    is "global::System.Runtime.CompilerServices.CallerMemberNameAttribute"
                    or "global::System.Runtime.CompilerServices.CallerArgumentExpressionAttribute") 
            == true)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.CompilerArgumentsPopulated,
                    argumentSyntax.GetLocation())
            );
        }
    }
}