using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using TUnit.Analyzers.Migrators.Base;

namespace TUnit.Analyzers.CodeFixers.Base;

public abstract class BaseMigrationCodeFixProvider : CodeFixProvider
{
    protected abstract string FrameworkName { get; }
    protected abstract string DiagnosticId { get; }
    protected abstract string CodeFixTitle { get; }
    
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
    
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();

        context.RegisterCodeFix(
            CodeAction.Create(
                title: CodeFixTitle,
                createChangedDocument: async c => await ConvertCodeAsync(context.Document, root, c),
                equivalenceKey: CodeFixTitle),
            diagnostic);
    }
    
    protected async Task<Document> ConvertCodeAsync(Document document, SyntaxNode? root, CancellationToken cancellationToken)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return document;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel == null)
        {
            return document;
        }

        try
        {
            // Convert assertions FIRST (while semantic model still matches the syntax tree)
            var assertionRewriter = CreateAssertionRewriter(semanticModel);
            compilationUnit = (CompilationUnitSyntax)assertionRewriter.Visit(compilationUnit);

            // Framework-specific conversions (also use semantic model while it still matches)
            compilationUnit = ApplyFrameworkSpecificConversions(compilationUnit, semanticModel);

            // Remove unnecessary base classes and interfaces
            var baseTypeRewriter = CreateBaseTypeRewriter(semanticModel);
            compilationUnit = (CompilationUnitSyntax)baseTypeRewriter.Visit(compilationUnit);

            // Update lifecycle methods
            var lifecycleRewriter = CreateLifecycleRewriter();
            compilationUnit = (CompilationUnitSyntax)lifecycleRewriter.Visit(compilationUnit);

            // Convert attributes
            var attributeRewriter = CreateAttributeRewriter();
            compilationUnit = (CompilationUnitSyntax)attributeRewriter.Visit(compilationUnit);

            // Remove framework usings and add TUnit usings (do this LAST)
            compilationUnit = MigrationHelpers.RemoveFrameworkUsings(compilationUnit, FrameworkName);
            compilationUnit = MigrationHelpers.AddTUnitUsings(compilationUnit);

            // Format the document first
            var documentWithNewRoot = document.WithSyntaxRoot(compilationUnit);
            var formattedDocument = await Formatter.FormatAsync(documentWithNewRoot, options: null, cancellationToken).ConfigureAwait(false);

            // Normalize all line endings to CRLF for cross-platform consistency
            var text = await formattedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var normalizedContent = text.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");
            var normalizedText = Microsoft.CodeAnalysis.Text.SourceText.From(normalizedContent, text.Encoding);

            return formattedDocument.WithText(normalizedText);
        }
        catch
        {
            // If any transformation fails, return the original document unchanged
            return document;
        }
    }
    
    protected abstract AttributeRewriter CreateAttributeRewriter();
    protected abstract CSharpSyntaxRewriter CreateAssertionRewriter(SemanticModel semanticModel);
    protected abstract CSharpSyntaxRewriter CreateBaseTypeRewriter(SemanticModel semanticModel);
    protected abstract CSharpSyntaxRewriter CreateLifecycleRewriter();
    protected abstract CompilationUnitSyntax ApplyFrameworkSpecificConversions(CompilationUnitSyntax compilationUnit, SemanticModel semanticModel);
}

public abstract class AttributeRewriter : CSharpSyntaxRewriter
{
    protected abstract string FrameworkName { get; }
    
    public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
    {
        var attributes = new List<AttributeSyntax>();

        foreach (var attribute in node.Attributes)
        {
            var attributeName = MigrationHelpers.GetAttributeName(attribute);

            if (MigrationHelpers.ShouldRemoveAttribute(attributeName, FrameworkName))
            {
                continue;
            }

            if (MigrationHelpers.IsHookAttribute(attributeName, FrameworkName))
            {
                var hookAttributeList = MigrationHelpers.ConvertHookAttribute(attribute, FrameworkName);
                if (hookAttributeList != null)
                {
                    // Preserve only the leading trivia (indentation) from the original node
                    // and strip any trailing trivia to prevent extra blank lines
                    return hookAttributeList
                        .WithLeadingTrivia(node.GetLeadingTrivia())
                        .WithTrailingTrivia(node.GetTrailingTrivia());
                }
            }

            var convertedAttribute = ConvertAttribute(attribute);
            if (convertedAttribute != null)
            {
                attributes.Add(convertedAttribute);
            }
        }

        return attributes.Count > 0
            ? node.WithAttributes(SyntaxFactory.SeparatedList(attributes))
            : null;
    }
    
    protected virtual AttributeSyntax? ConvertAttribute(AttributeSyntax attribute)
    {
        var attributeName = MigrationHelpers.GetAttributeName(attribute);
        var newName = MigrationHelpers.ConvertTestAttributeName(attributeName, FrameworkName);
        
        if (newName == null)
        {
            return null;
        }
        
        if (newName == attributeName && !IsFrameworkAttribute(attributeName))
        {
            return attribute;
        }
        
        var newAttribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(newName));
        
        if (attribute.ArgumentList != null && attribute.ArgumentList.Arguments.Count > 0)
        {
            newAttribute = newAttribute.WithArgumentList(ConvertAttributeArguments(attribute.ArgumentList, attributeName));
        }
        
        return newAttribute;
    }
    
    protected abstract bool IsFrameworkAttribute(string attributeName);
    protected abstract AttributeArgumentListSyntax? ConvertAttributeArguments(AttributeArgumentListSyntax argumentList, string attributeName);
}