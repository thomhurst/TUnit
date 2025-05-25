using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class XUnitMigrationAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.XunitMigration);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ClassDeclaration);
    }
    
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        if(context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }
        
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        
        if (symbol is null)
        {
            return;
        }
        
        if (symbol.AllInterfaces.Any(i => i.ContainingNamespace.Name.StartsWith("Xunit")))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.XunitMigration, context.Node.GetLocation())
            );
            
            return;
        }
        
        if (AnalyzeAttributes(context, symbol)) 
        {
            return;
        }
        
        foreach (var methodSymbol in symbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (AnalyzeAttributes(context, methodSymbol))
            {
                return;
            }
        }
        
        var usingDirectiveSyntaxes = classDeclarationSyntax
            .SyntaxTree
            .GetCompilationUnitRoot()
            .Usings;
        
        foreach (var usingDirectiveSyntax in usingDirectiveSyntaxes)
        {
            if (usingDirectiveSyntax.Name is QualifiedNameSyntax { Left: IdentifierNameSyntax { Identifier.Text: "Xunit" } }
                or IdentifierNameSyntax { Identifier.Text: "Xunit" })
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.XunitMigration, context.Node.GetLocation()));
                return;
            }
        }
    }

    private bool AnalyzeAttributes(SyntaxNodeAnalysisContext context, ISymbol symbol)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            var @namespace = attributeData.AttributeClass?.ContainingNamespace?.Name;
            
            if(@namespace == "Xunit")
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.XunitMigration, context.Node.GetLocation())
                );

                return true;
            }
        }

        return false;
    }
}