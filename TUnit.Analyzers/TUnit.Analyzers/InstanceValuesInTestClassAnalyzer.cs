using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InstanceValuesInTestClassAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.InstanceDataInTestClass);

    public override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ClassDeclaration);
    }
    
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    { 
        if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
       
        if (symbol is not { } namedTypeSymbol)
        {
            return;
        }

        var classMembers = namedTypeSymbol.GetMembers();
        
        var tests = classMembers
            .OfType<IMethodSymbol>()
            .Where(x => x.GetAttributes()
                .Any(a => WellKnown.AttributeFullyQualifiedClasses.TestAttributes.Contains(
                    a.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix))))
            .ToList();

        if (!tests.Any())
        {
            return;
        }

        var fieldsAndProperties = classMembers
            .OfType<IFieldSymbol>()
            .Concat<ISymbol>(classMembers.OfType<IPropertySymbol>())
            .Where(x => !x.IsStatic);
        
        foreach (var fieldOrProperty in fieldsAndProperties)
        {
            var fieldOrPropertySyntax = fieldOrProperty.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

            if (tests
                    .Select(x => x.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax())
                    .OfType<MethodDeclarationSyntax>()
                    .Count(x => x.DescendantNodes().Any(n => IsMatching(context, n, fieldOrPropertySyntax)))
                > 1)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.InstanceDataInTestClass,
                        fieldOrProperty.Locations.FirstOrDefault())
                );
            }
        }
    }

    private bool IsMatching(SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode, SyntaxNode? fieldOrPropertySyntax)
    {
        if (syntaxNode is not MemberAccessExpressionSyntax memberAccessExpressionSyntax
            || fieldOrPropertySyntax is null)
        {
            return false;
        }

        var symbol = context.SemanticModel.GetDeclaredSymbol(fieldOrPropertySyntax);

        var field = symbol as IFieldSymbol;
        var property = symbol as IPropertySymbol;

        var memberAccessName = memberAccessExpressionSyntax.Name.Identifier.FullSpan.ToString();
        if (memberAccessName == field?.ToDisplayString()
            || memberAccessName == property?.ToDisplayString())
        {
            return true;
        }

        return false;
    }
}