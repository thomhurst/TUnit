using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using TUnit.Assertions.Analyzers.CodeFixers.Extensions;

namespace TUnit.Assertions.Analyzers.CodeFixers;

/// <summary>
/// A sample code fix provider that renames classes with the company name in their definition.
/// All code fixes must  be linked to specific analyzers.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AwaitAssertionCodeFixProvider)), Shared]
public class AwaitAssertionCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.AwaitAssertion.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnosticNode = root?.FindNode(diagnosticSpan);

            if (diagnosticNode is not ExpressionSyntax expressionSyntax)
            {
                return;
            }

            var upperMostExpression = expressionSyntax.AncestorsAndSelf().OfType<ExpressionSyntax>().Last();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Resources.TUnitAssertions0002CodeFixTitle,
                    createChangedDocument: c => AwaitAssertionAsync(context.Document, upperMostExpression, c),
                    equivalenceKey: nameof(Resources.TUnitAssertions0002CodeFixTitle)),
                diagnostic);
        }
    }

    /// <summary>
    /// Executed on the quick fix action raised by the user.
    /// </summary>
    /// <param name="document">Affected source file.</param>
    /// <param name="expressionSyntax">Highlighted class declaration Syntax Node.</param>
    /// <param name="cancellationToken">Any fix is cancellable by the user, so we should support the cancellation token.</param>
    /// <returns>Clone of the solution with updates: renamed class.</returns>
    private static async Task<Document> AwaitAssertionAsync(Document document, ExpressionSyntax expressionSyntax, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        await editor.AddUsingDirective("System.Threading.Tasks");
        
        // Add await to the invocation expression
        var awaitExpression = SyntaxFactory.AwaitExpression(expressionSyntax.WithLeadingTrivia(SyntaxFactory.Space))
            .WithLeadingTrivia(expressionSyntax.GetLeadingTrivia());
        
        // Find the containing method
        var methodDeclaration = expressionSyntax.AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (methodDeclaration == null)
        {
            return editor.GetChangedDocument();
        }

        var modifiers = methodDeclaration.Modifiers;
        
        var returnType = methodDeclaration.ReturnType;
        var newReturnType = returnType;
        
        // Check if the method is already async
        if (!methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            // Add async modifier
            var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);
            modifiers = methodDeclaration.Modifiers.Add(asyncModifier
                .WithTrailingTrivia(SyntaxFactory.Space));

            // Update the return type to Task or Task<T>
            if (returnType is PredefinedTypeSyntax predefinedType &&
                predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword))
            {
                newReturnType = SyntaxFactory.IdentifierName("Task")
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }
            else if (returnType is not GenericNameSyntax genericName || genericName.Identifier.Text != "Task")
            {
                newReturnType = SyntaxFactory.ParseTypeName($"Task<{returnType}>")
                    .WithTrailingTrivia(SyntaxFactory.Space);
            }
        }

        var newMethodDeclaration = methodDeclaration
            .ReplaceNode(expressionSyntax, awaitExpression)
            .WithModifiers(modifiers)
            .WithReturnType(newReturnType)
            .WithAdditionalAnnotations(Formatter.Annotation);

        editor.ReplaceNode(methodDeclaration, newMethodDeclaration);

        return editor.GetChangedDocument();
    }
}