using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        var compilation = semanticModel.Compilation;

        try
        {
            // Convert assertions FIRST (while semantic model still matches the syntax tree)
            var assertionRewriter = CreateAssertionRewriter(semanticModel, compilation);
            compilationUnit = (CompilationUnitSyntax)assertionRewriter.Visit(compilationUnit);

            // Framework-specific conversions (also use semantic model while it still matches)
            compilationUnit = ApplyFrameworkSpecificConversions(compilationUnit, semanticModel, compilation);

            // Remove unnecessary base classes and interfaces
            var baseTypeRewriter = CreateBaseTypeRewriter(semanticModel, compilation);
            compilationUnit = (CompilationUnitSyntax)baseTypeRewriter.Visit(compilationUnit);

            // Update lifecycle methods
            var lifecycleRewriter = CreateLifecycleRewriter(compilation);
            compilationUnit = (CompilationUnitSyntax)lifecycleRewriter.Visit(compilationUnit);

            // Convert attributes
            var attributeRewriter = CreateAttributeRewriter(compilation);
            compilationUnit = (CompilationUnitSyntax)attributeRewriter.Visit(compilationUnit);

            // Remove framework usings and add TUnit usings (do this LAST)
            compilationUnit = MigrationHelpers.RemoveFrameworkUsings(compilationUnit, FrameworkName);

            if (ShouldAddTUnitUsings())
            {
                compilationUnit = MigrationHelpers.AddTUnitUsings(compilationUnit);
            }

            // Clean up trivia issues that can occur after transformations
            compilationUnit = CleanupClassMemberLeadingTrivia(compilationUnit);
            compilationUnit = CleanupEndOfFileTrivia(compilationUnit);

            // Return the document with updated syntax root, preserving original formatting
            return document.WithSyntaxRoot(compilationUnit);
        }
        catch
        {
            // If any transformation fails, return the original document unchanged
            return document;
        }
    }

    protected abstract AttributeRewriter CreateAttributeRewriter(Compilation compilation);
    protected abstract CSharpSyntaxRewriter CreateAssertionRewriter(SemanticModel semanticModel, Compilation compilation);
    protected abstract CSharpSyntaxRewriter CreateBaseTypeRewriter(SemanticModel semanticModel, Compilation compilation);
    protected abstract CSharpSyntaxRewriter CreateLifecycleRewriter(Compilation compilation);
    protected abstract CompilationUnitSyntax ApplyFrameworkSpecificConversions(CompilationUnitSyntax compilationUnit, SemanticModel semanticModel, Compilation compilation);

    /// <summary>
    /// Determines whether to add TUnit usings (including assertion usings).
    /// Override to return false for frameworks that don't need assertion usings (e.g., XUnit).
    /// </summary>
    protected virtual bool ShouldAddTUnitUsings() => true;

    /// <summary>
    /// Removes excessive blank lines at the start of class members (after opening brace).
    /// This can occur after removing members like ITestOutputHelper fields/properties.
    /// </summary>
    protected static CompilationUnitSyntax CleanupClassMemberLeadingTrivia(CompilationUnitSyntax root)
    {
        var classesToFix = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c => c.Members.Any())
            .ToList();

        var currentRoot = root;
        foreach (var classDecl in classesToFix)
        {
            var firstMember = classDecl.Members.First();
            var leadingTrivia = firstMember.GetLeadingTrivia();
            int newlineCount = leadingTrivia.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia));

            if (newlineCount > 0)
            {
                // Keep only indentation (whitespace), remove all newlines
                var triviaToKeep = leadingTrivia
                    .Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia))
                    .Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia) ||
                                (!t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
                    .ToList();

                var newFirstMember = firstMember.WithLeadingTrivia(triviaToKeep);
                var updatedClass = classDecl.ReplaceNode(firstMember, newFirstMember);
                currentRoot = currentRoot.ReplaceNode(classDecl, updatedClass);
            }
        }

        return (CompilationUnitSyntax)currentRoot;
    }

    /// <summary>
    /// Removes trailing blank lines at end of file.
    /// Files should end immediately after the closing brace with no trailing newlines.
    /// </summary>
    protected static CompilationUnitSyntax CleanupEndOfFileTrivia(CompilationUnitSyntax compilationUnit)
    {
        var lastMember = compilationUnit.Members.LastOrDefault();
        if (lastMember != null)
        {
            var trailingTrivia = lastMember.GetTrailingTrivia();
            int newlineCount = trailingTrivia.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia));

            if (newlineCount > 0)
            {
                var newTrivia = trailingTrivia
                    .Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia))
                    .ToList();

                var newLastMember = lastMember.WithTrailingTrivia(newTrivia);
                return compilationUnit.ReplaceNode(lastMember, newLastMember);
            }
        }

        return compilationUnit;
    }
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