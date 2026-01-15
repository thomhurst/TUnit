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
            // IMPORTANT: Collect interface-implementing methods BEFORE any syntax modifications
            // while the semantic model is still valid for the original syntax tree
            var interfaceImplementingMethods = AsyncMethodSignatureRewriter.CollectInterfaceImplementingMethods(
                compilationUnit, semanticModel);

            // Convert assertions FIRST (while semantic model still matches the syntax tree)
            var assertionRewriter = CreateAssertionRewriter(semanticModel, compilation);
            compilationUnit = (CompilationUnitSyntax)assertionRewriter.Visit(compilationUnit);

            // Framework-specific conversions (also use semantic model while it still matches)
            compilationUnit = ApplyFrameworkSpecificConversions(compilationUnit, semanticModel, compilation);

            // Fix method signatures that now contain await but aren't marked async
            // Pass the collected interface methods to avoid converting interface implementations
            var asyncSignatureRewriter = new AsyncMethodSignatureRewriter(interfaceImplementingMethods);
            compilationUnit = (CompilationUnitSyntax)asyncSignatureRewriter.Visit(compilationUnit);

            // Remove unnecessary base classes and interfaces
            var baseTypeRewriter = CreateBaseTypeRewriter(semanticModel, compilation);
            compilationUnit = (CompilationUnitSyntax)baseTypeRewriter.Visit(compilationUnit);

            // Update lifecycle methods
            var lifecycleRewriter = CreateLifecycleRewriter(compilation);
            compilationUnit = (CompilationUnitSyntax)lifecycleRewriter.Visit(compilationUnit);

            // Convert attributes
            var attributeRewriter = CreateAttributeRewriter(compilation);
            compilationUnit = (CompilationUnitSyntax)attributeRewriter.Visit(compilationUnit);

            // Ensure [Test] attribute is present when data attributes exist (NUnit-specific)
            if (ShouldEnsureTestAttribute())
            {
                var testAttributeEnsurer = new TestAttributeEnsurer();
                compilationUnit = (CompilationUnitSyntax)testAttributeEnsurer.Visit(compilationUnit);
            }

            // Remove framework usings and add TUnit usings (do this LAST)
            compilationUnit = MigrationHelpers.RemoveFrameworkUsings(compilationUnit, FrameworkName);

            if (ShouldAddTUnitUsings())
            {
                compilationUnit = MigrationHelpers.AddTUnitUsings(compilationUnit);
            }
            else
            {
                // Even if not adding TUnit usings, always add System.Threading.Tasks if there's async code
                compilationUnit = MigrationHelpers.AddSystemThreadingTasksUsing(compilationUnit);
            }

            // Clean up trivia issues that can occur after transformations
            compilationUnit = CleanupClassMemberLeadingTrivia(compilationUnit);
            compilationUnit = CleanupEndOfFileTrivia(compilationUnit);

            // Normalize line endings to match original document (fixes cross-platform issues)
            compilationUnit = NormalizeLineEndings(compilationUnit, root);

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
    /// Determines whether to run TestAttributeEnsurer to add [Test] when data attributes exist.
    /// Override to return true for NUnit (where [TestCase] alone is valid but TUnit requires [Test] + [Arguments]).
    /// Default is false since most frameworks don't need this.
    /// </summary>
    protected virtual bool ShouldEnsureTestAttribute() => false;

    /// <summary>
    /// Removes excessive blank lines at the start of class members (after opening brace).
    /// This can occur after removing members like ITestOutputHelper fields/properties.
    /// </summary>
    protected static CompilationUnitSyntax CleanupClassMemberLeadingTrivia(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        // Use a while loop to re-query after each modification.
        // This is necessary because ReplaceNode returns a new tree, and node references
        // from the original tree won't match nodes in the new tree.
        ClassDeclarationSyntax? classToFix;
        while ((classToFix = FindClassWithExcessiveLeadingTrivia(currentRoot)) != null)
        {
            var firstMember = classToFix.Members.First();
            var leadingTrivia = firstMember.GetLeadingTrivia();

            // Keep only indentation (whitespace), remove all newlines
            var triviaToKeep = leadingTrivia
                .Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia))
                .Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia) ||
                            (!t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
                .ToList();

            var newFirstMember = firstMember.WithLeadingTrivia(triviaToKeep);
            var updatedClass = classToFix.ReplaceNode(firstMember, newFirstMember);
            currentRoot = currentRoot.ReplaceNode(classToFix, updatedClass);
        }

        return currentRoot;
    }

    /// <summary>
    /// Finds a class with excessive leading trivia on its first member.
    /// Returns null if no such class exists.
    /// </summary>
    private static ClassDeclarationSyntax? FindClassWithExcessiveLeadingTrivia(CompilationUnitSyntax root)
    {
        return root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => c.Members.Any())
            .FirstOrDefault(c =>
            {
                var leadingTrivia = c.Members.First().GetLeadingTrivia();
                return leadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
            });
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

    /// <summary>
    /// Normalizes all line endings in the compilation unit to LF (Unix format) for cross-platform consistency.
    /// This ensures code fixes produce consistent output regardless of the platform or original document's line endings,
    /// preventing test failures between Windows (CRLF) and Unix (LF) systems.
    /// </summary>
    private static CompilationUnitSyntax NormalizeLineEndings(CompilationUnitSyntax compilationUnit, SyntaxNode originalRoot)
    {
        // Always normalize to LF for cross-platform consistency
        // This matches our test infrastructure which also normalizes to LF
        const string targetEol = "\n";

        // Create a rewriter that replaces all EndOfLine trivia with the target line ending
        var rewriter = new LineEndingNormalizer(targetEol);
        return (CompilationUnitSyntax)rewriter.Visit(compilationUnit);
    }

    /// <summary>
    /// Rewrites all EndOfLine trivia to use a consistent line ending style.
    /// </summary>
    private class LineEndingNormalizer(string lineEnding) : CSharpSyntaxRewriter(visitIntoStructuredTrivia: true)
    {
        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return SyntaxFactory.EndOfLine(lineEnding);
            }

            return base.VisitTrivia(trivia);
        }
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
                    // Add converted hook attribute(s) to the list - don't return early!
                    // This preserves other attributes that may be in the same attribute list.
                    // e.g., [SetUp, Category("Unit")] -> [Before(HookType.Test), Category("Unit")]
                    attributes.AddRange(hookAttributeList.Attributes);
                    continue;
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
                .WithLeadingTrivia(node.GetLeadingTrivia())
                .WithTrailingTrivia(node.GetTrailingTrivia())
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