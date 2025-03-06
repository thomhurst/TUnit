using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class XUnitUsingDirectiveAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.XunitUsingDirectives);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.UsingDirective);
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not UsingDirectiveSyntax usingDirectiveSyntax)
        {
            return;
        }

        if (usingDirectiveSyntax.Name is QualifiedNameSyntax { Left: IdentifierNameSyntax { Identifier.Text: "Xunit" } }
            or IdentifierNameSyntax { Identifier.Text: "Xunit" })
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.XunitUsingDirectives, usingDirectiveSyntax.GetLocation()));
        }
    }
}