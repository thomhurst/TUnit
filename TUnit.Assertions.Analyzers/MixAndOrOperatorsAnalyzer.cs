using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Assertions.Analyzers.Extensions;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// A sample analyzer that reports the company name being used in class declarations.
/// Traverses through the Syntax Tree and checks the name (identifier) of each class node.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MixAndOrOperatorsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.MixAndOrConditionsAssertion);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SimpleMemberAccessExpression);
    }
    
    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        // The Roslyn architecture is based on inheritance.
        // To get the required metadata, we should match the 'Node' object to the particular type: 'ClassDeclarationSyntax'.
        if (context.Node is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            return;
        }

        if (memberAccessExpressionSyntax.Name.Identifier.Value is not "And" and not "Or")
        {
            return;
        }

        if (memberAccessExpressionSyntax.GetAncestorSyntaxOfType<InvocationExpressionSyntax>() is not { } invocationExpressionSyntax)
        {
            return;
        }
        
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax);

        var fullyQualifiedSymbolInformation = symbolInfo.Symbol?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) 
                                              ?? string.Empty;
        
        if (!fullyQualifiedSymbolInformation.StartsWith("global::TUnit.Assertions"))
        {
            return;
        }

        var fullInvocationStatement = invocationExpressionSyntax.ToFullString();
        if(fullInvocationStatement.Contains(".And.") 
           && fullInvocationStatement.Contains(".Or."))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MixAndOrConditionsAssertion,
                    invocationExpressionSyntax.GetLocation())
            );
        }
    }
}