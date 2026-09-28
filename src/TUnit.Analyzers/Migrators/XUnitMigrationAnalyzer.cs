using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Migrators.Base;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class XUnitMigrationAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.XunitMigration);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            // Every semantic check below matches symbols whose namespace name starts with "Xunit". When no such
            // namespace exists in the compilation, only the syntactic using-directive checks can report,
            // so skip binding every class in the project.
            var canContainXunitSymbols = MigrationNamespaceHelper.ContainsNamespace(
                compilationStartContext.Compilation,
                "Xunit",
                IsXunitNamespace);

            compilationStartContext.RegisterSyntaxNodeAction(
                syntaxNodeContext => AnalyzeSyntax(syntaxNodeContext, canContainXunitSymbols),
                SyntaxKind.CompilationUnit);
        });
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context, bool canContainXunitSymbols)
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
            if (!canContainXunitSymbols)
            {
                // The symbol checks can only match Xunit namespaces, so only the using directives remain.
                if (HasXunitUsing(classDeclarationSyntax))
                {
                    Flag(context);
                    return;
                }

                continue;
            }

            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            if (symbol is null)
            {
                return;
            }

            if (symbol.AllInterfaces.Any(i => IsXunitNamespace(i.ContainingNamespace)))
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

            if (HasXunitUsing(classDeclarationSyntax))
            {
                Flag(context);
                return;
            }

            var members = symbol.GetMembers();

            var types = members.OfType<IPropertySymbol>().Where(x => IsXunitNamespace(x.Type.ContainingNamespace)).Select(x => x.Type)
                .Concat(members.OfType<IMethodSymbol>().Where(x => IsXunitNamespace(x.ReturnType.ContainingNamespace)).Select(x => x.ReturnType))
                .Concat(members.OfType<IFieldSymbol>().Where(x => IsXunitNamespace(x.Type.ContainingNamespace)).Select(x => x.Type))
                .ToArray();

            if (types.Any())
            {
                Flag(context);
                return;
            }
        }

        // Check for global usings at the compilation unit level
        // This handles files like GlobalUsings.cs that have no classes
        foreach (var usingDirective in compilationUnitSyntax.Usings)
        {
            if (!usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)
                || usingDirective.Name is not (
                    QualifiedNameSyntax
                    {
                        Left: IdentifierNameSyntax {Identifier.Text: "Xunit"}
                    }
                    or IdentifierNameSyntax
                    {
                        Identifier.Text: "Xunit"
                    }))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rules.XunitMigration, usingDirective.GetLocation()));
            return;
        }
    }

    private static bool HasXunitUsing(ClassDeclarationSyntax classDeclarationSyntax)
    {
        var usingDirectiveSyntaxes = classDeclarationSyntax
            .SyntaxTree
            .GetCompilationUnitRoot()
            .Usings;

        foreach (var usingDirectiveSyntax in usingDirectiveSyntaxes)
        {
            if (usingDirectiveSyntax.Name is QualifiedNameSyntax { Left: IdentifierNameSyntax { Identifier.Text: "Xunit" } }
                or IdentifierNameSyntax { Identifier.Text: "Xunit" })
            {
                return true;
            }
        }

        return false;
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

    /// <summary>
    /// The single namespace test shared by the compilation-start gate and the semantic checks, so they cannot drift apart.
    /// Ordinal on purpose: a culture-sensitive comparison ignores or combines some characters, so its result would
    /// depend on the current culture and not match the exact namespace names the gate walks.
    /// </summary>
    private static bool IsXunitNamespace(INamespaceSymbol? ns)
    {
        return ns?.Name.StartsWith("Xunit", StringComparison.Ordinal) is true;
    }

    private static void Flag(SyntaxNodeAnalysisContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(Rules.XunitMigration, context.Node.GetLocation()));
    }
}
