using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Assertions.Analyzers.Extensions;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// A sample analyzer that reports the company name being used in class declarations.
/// Traverses through the Syntax Tree and checks the name (identifier) of each class node.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConstantInAssertThatAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.ConstantValueInAssertThat);

    public override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
    }
    
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocationExpressionSyntax)
        {
            return;
        }

        var operation = context.SemanticModel.GetOperation(invocationExpressionSyntax);

        if (operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        if (invocationOperation.TargetMethod.Name != "That"
            || invocationOperation.TargetMethod.ContainingType?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) != "global::TUnit.Assertions.Assert")
        {
            return;
        }
        
        // True if constant
        var firstArgument = invocationOperation.Arguments[0];
        if (firstArgument.ConstantValue.HasValue || firstArgument.Value.ConstantValue.HasValue)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.ConstantValueInAssertThat, invocationExpressionSyntax.GetLocation())
            );
        }
    }
}