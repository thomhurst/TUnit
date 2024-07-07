using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConflictingTestAttributesAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.ConflictingTestAttributes, Rules.Wrong_Category_Attribute);

    protected override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.MethodDeclaration);
    }
    
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    { 
        if (context.Node is not MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax)
            is not { } methodSymbol)
        {
            return;
        }

        var attributeDatas = methodSymbol.GetAttributes();
        var testAttributesCount = attributeDatas.Count(x => WellKnown.AttributeFullyQualifiedClasses.TestAttributes.Contains(x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)));

        if (testAttributesCount == 0)
        {
            return;
        }

        if (testAttributesCount > 1)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.ConflictingTestAttributes,
                    methodDeclarationSyntax.GetLocation())
            );
        }

        foreach (var invalidCategoryAttribute in attributeDatas.Where(x =>
                     x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ==
                     $"global::{typeof(CategoryAttribute).FullName}"))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.Wrong_Category_Attribute,
                    invalidCategoryAttribute.GetLocation() ?? methodDeclarationSyntax.GetLocation())
            );
        }
    }
}