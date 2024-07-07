using System.Collections.Immutable;
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
        ImmutableArray.Create(Rules.InstanceAssignmentInTestClass);

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

            var methodDeclarationSyntaxes = tests
                .Select(x => x.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax())
                .OfType<MethodDeclarationSyntax>();
            
            foreach (var methodDeclarationSyntax in methodDeclarationSyntaxes)
            {
                CheckMethod(context, methodDeclarationSyntax, fieldOrPropertySyntax);
            }
        }
    }

    private void CheckMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclarationSyntax,
        SyntaxNode? fieldOrPropertySyntax)
    {
        var descendantNodes = methodDeclarationSyntax.DescendantNodes();
        
        foreach (var descendantNode in descendantNodes)
        {
            if (IsAssignment(descendantNode, fieldOrPropertySyntax))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.InstanceAssignmentInTestClass,
                        descendantNode.GetLocation())
                );
            }
        }
    }

    private bool IsAssignment(SyntaxNode syntaxNode, SyntaxNode? fieldOrPropertySyntax)
    {
        if (syntaxNode is not AssignmentExpressionSyntax assignmentExpressionSyntax)
        {
            return false;
        }

        var assignmentSyntaxChild = assignmentExpressionSyntax.ChildNodes().FirstOrDefault();

        if (assignmentSyntaxChild is not IdentifierNameSyntax identifierNameSyntax)
        {
            return false;
        }

        return identifierNameSyntax.Identifier.ValueText == fieldOrPropertySyntax?.ToString();
    }
}