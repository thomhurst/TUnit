using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetUpCleanUpMethodsAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.MethodMustBeParameterless, 
            Rules.MethodMustNotBeAbstract, 
            Rules.MethodMustNotBeStatic,
            Rules.MethodMustBePublic);

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

        var attributes = methodSymbol.GetAttributes();

        var onlyOnceAttributes = attributes.Where(x =>
            x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is "global::TUnit.Core.BeforeEachTestAttribute"
                or "global::TUnit.Core.AfterEachTestAttribute")
            .ToList();

        if (!onlyOnceAttributes.Any())
        {
            return;
        }

        if (methodSymbol.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustNotBeStatic,
                methodDeclarationSyntax.GetLocation())
            );
        }
        
                
        if(methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustBePublic,
                methodDeclarationSyntax.GetLocation())
            );
        }

        if (methodSymbol.IsAbstract)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustNotBeAbstract,
                methodDeclarationSyntax.GetLocation())
            );
        }

        if (!methodSymbol.Parameters.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustBeParameterless,
                methodDeclarationSyntax.GetLocation())
            );
        }
    }
}