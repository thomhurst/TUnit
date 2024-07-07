using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using AttributeUsageAttribute = System.AttributeUsageAttribute;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ForbidRedefiningAttributeUsageAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.DoNotOverrideAttributeUsageMetadata
            );

    protected override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ClassDeclaration);
    }
    
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    { 
        if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax)
            is not { } namedTypeSymbol)
        {
            return;
        }

        if (namedTypeSymbol.GetSelfAndBaseTypes().All(x => x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) != "global::TUnit.Core.TUnitAttribute"))
        {
            return;
        }

        var attributeUsage = namedTypeSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) == $"global::{typeof(AttributeUsageAttribute).FullName}");

        if (attributeUsage == null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.DoNotOverrideAttributeUsageMetadata, context.Node.GetLocation()));
    }
}