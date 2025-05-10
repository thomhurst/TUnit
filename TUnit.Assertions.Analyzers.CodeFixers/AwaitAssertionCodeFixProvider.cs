using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using TUnit.Assertions.Analyzers.CodeFixers.Extensions;

namespace TUnit.Assertions.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AwaitAssertionCodeFixProvider)), Shared]
public class AwaitAssertionCodeFixProvider : CodeFixProvider
{
    public override sealed ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.AwaitAssertion.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override sealed async Task RegisterCodeFixesAsync(CodeFixContext context)
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

    private static async Task<Document> AwaitAssertionAsync(Document document, ExpressionSyntax expressionSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        var awaitExpression = SyntaxFactory.AwaitExpression(expressionSyntax.WithLeadingTrivia(SyntaxFactory.Space))
            .WithLeadingTrivia(expressionSyntax.GetLeadingTrivia());

        var methodDeclaration = expressionSyntax.AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (methodDeclaration == null)
        {
            return document.WithSyntaxRoot(root);
        }

        var modifiers = methodDeclaration.Modifiers;
        var returnType = methodDeclaration.ReturnType;
        var newReturnType = returnType;

        if (!methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);
            modifiers = methodDeclaration.Modifiers.Add(asyncModifier.WithTrailingTrivia(SyntaxFactory.Space));

            if (returnType is PredefinedTypeSyntax predefinedType && predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword))
            {
                newReturnType = SyntaxFactory.IdentifierName("Task").WithTrailingTrivia(SyntaxFactory.Space);
            }
            else if (returnType is not GenericNameSyntax genericName || genericName.Identifier.Text != "Task")
            {
                newReturnType = SyntaxFactory.ParseTypeName($"Task<{returnType}>").WithTrailingTrivia(SyntaxFactory.Space);
            }
        }

        var newMethodDeclaration = methodDeclaration
            .ReplaceNode(expressionSyntax, awaitExpression)
            .WithModifiers(modifiers)
            .WithReturnType(newReturnType)
            .WithAdditionalAnnotations(Formatter.Annotation);

        root = root.ReplaceNode(methodDeclaration, newMethodDeclaration);

        if (root is CompilationUnitSyntax compilationUnit)
        {
            root = await document.AddUsingDirectiveIfNotExistsAsync(compilationUnit, "System.Threading.Tasks", cancellationToken);
        }

        return document.WithSyntaxRoot(root);
    }
}