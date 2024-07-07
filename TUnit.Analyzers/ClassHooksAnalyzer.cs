using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ClassHooksAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.MethodMustBeParameterless, Rules.MethodMustNotBeAbstract, Rules.MethodMustBeStatic, Rules.MethodMustBePublic, Rules.UnknownParameters);

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
                is WellKnown.AttributeFullyQualifiedClasses.BeforeAllTestsInClassAttribute
                or WellKnown.AttributeFullyQualifiedClasses.AfterAllTestsInClassAttribute
            )
            .ToList();

        if (!onlyOnceAttributes.Any())
        {
            return;
        }

        if (!methodSymbol.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustBeStatic,
                methodDeclarationSyntax.GetLocation())
            );
        }

        if (methodSymbol.IsAbstract)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustNotBeAbstract,
                methodDeclarationSyntax.GetLocation())
            );
        }
        
        if(methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MethodMustBePublic,
                methodDeclarationSyntax.GetLocation())
            );
        }
            
        if (!IsClassHookContextParameter(methodSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.UnknownParameters,
                methodDeclarationSyntax.GetLocation(),
                "empty or only contain `ClassHookContext`")
            );
        }
    }

    private static bool IsClassHookContextParameter(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.Parameters.IsDefaultOrEmpty)
        {
            return true;
        }

        foreach (var parameter in methodSymbol.Parameters)
        {
            if (parameter.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ==
                WellKnown.AttributeFullyQualifiedClasses.ClassHookContext)
            {
                continue;
            }

            if (parameter.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ==
                WellKnown.AttributeFullyQualifiedClasses.CancellationToken)
            {
                continue;
            }

            return false;
        }

        return true;
    }
}