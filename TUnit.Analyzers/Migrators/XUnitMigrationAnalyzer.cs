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
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.CompilationUnit);
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not CompilationUnitSyntax compilationUnitSyntax)
        {
            return;
        }

        var classDeclarationSyntaxes = compilationUnitSyntax
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>();

        foreach (var classDeclarationSyntax in classDeclarationSyntaxes)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            if (symbol is null)
            {
                return;
            }

            if (symbol.AllInterfaces.Any(i => i.ContainingNamespace?.Name.StartsWith("Xunit") is true))
            {
                Flag(context);
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
                    Flag(context);
                    return;
                }
            }

            var namedTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            if (namedTypeSymbol is null)
            {
                return;
            }

            var members = namedTypeSymbol.GetMembers();

            var types = members.OfType<IPropertySymbol>().Where(x => x.Type.ContainingNamespace?.Name.StartsWith("Xunit") is true).Select(x => x.Type)
                .Concat(members.OfType<IMethodSymbol>().Where(x => x.ReturnType.ContainingNamespace?.Name.StartsWith("Xunit") is true).Select(x => x.ReturnType))
                .Concat(members.OfType<IFieldSymbol>().Where(x => x.Type.ContainingNamespace?.Name.StartsWith("Xunit") is true).Select(x => x.Type))
                .ToArray();

            if (types.Any())
            {
                Flag(context);
                return;
            }
        }
    }

    private bool AnalyzeAttributes(SyntaxNodeAnalysisContext context, ISymbol symbol)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            var @namespace = attributeData.AttributeClass?.ContainingNamespace?.Name;

            if (@namespace == "Xunit")
            {
                Flag(context);
                return true;
            }
        }

        return false;
    }

    private static void Flag(SyntaxNodeAnalysisContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(Rules.XunitMigration, context.Node.GetLocation()));
    }
}
