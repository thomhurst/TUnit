using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace TUnit.Analyzers;

/// <summary>
/// A sample code fix provider that renames classes with the company name in their definition.
/// All code fixes must  be linked to specific analyzers.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SampleCodeFixProvider)), Shared]
public class SampleCodeFixProvider : CodeFixProvider
{
    private const string CommonName = "Common";

    // Specify the diagnostic IDs of analyzers that are expected to be linked.
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(SampleSyntaxAnalyzer.DiagnosticId);

    // If you don't need the 'fix all' behaviour, return null.
    public override FixAllProvider? GetFixAllProvider() => null;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        // We link only one diagnostic and assume there is only one diagnostic in the context.
        var diagnostic = context.Diagnostics.Single();

        // 'SourceSpan' of 'Location' is the highlighted area. We're going to use this area to find the 'SyntaxNode' to rename.
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Get the root of Syntax Tree that contains the highlighted diagnostic.
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        // Find SyntaxNode corresponding to the diagnostic.
        var diagnosticNode = root?.FindNode(diagnosticSpan);

        // To get the required metadata, we should match the Node to the specific type: 'ClassDeclarationSyntax'.
        if (diagnosticNode is not ClassDeclarationSyntax declaration)
            return;

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                title: string.Format(Resources.TUnit0001CodeFixTitle, SampleSyntaxAnalyzer.CompanyName, CommonName),
                createChangedSolution: c => SanitizeCompanyNameAsync(context.Document, declaration, c),
                equivalenceKey: nameof(Resources.TUnit0001CodeFixTitle)),
            diagnostic);
    }

    /// <summary>
    /// Executed on the quick fix action raised by the user.
    /// </summary>
    /// <param name="document">Affected source file.</param>
    /// <param name="classDeclarationSyntax">Highlighted class declaration Syntax Node.</param>
    /// <param name="cancellationToken">Any fix is cancellable by the user, so we should support the cancellation token.</param>
    /// <returns>Clone of the solution with updates: renamed class.</returns>
    private async Task<Solution> SanitizeCompanyNameAsync(Document document,
        ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
    {
        // 'Identifier' means the token of the node. Compute the new name based on the text of the token of the node.
        var newName = classDeclarationSyntax.Identifier.Text.Replace(SampleSyntaxAnalyzer.CompanyName, CommonName);

        // To make a refactoring, we need to get compiled code metadata: the Semantic Model.
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // Attempt to find the 'TypeSymbol' (compile time metadata of the class) based on highlighted Class Declaration Syntax.
        var typeSymbol = semanticModel?.GetDeclaredSymbol(classDeclarationSyntax, cancellationToken);
        if (typeSymbol == null) return document.Project.Solution;

        // Produce a new solution that has all references to the class being renamed, including the declaration.
        var newSolution = await Renamer
            .RenameSymbolAsync(document.Project.Solution, typeSymbol, new SymbolRenameOptions(), newName,
                cancellationToken)
            .ConfigureAwait(false);

        // Return the new solution with the updated type name.
        return newSolution;
    }
}