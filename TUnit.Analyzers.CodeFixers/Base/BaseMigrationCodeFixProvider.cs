using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.CodeFixers.Base.TwoPhase;
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

        // Check if this framework supports the new two-phase architecture
        var analyzer = CreateTwoPhaseAnalyzer(semanticModel, compilation);
        if (analyzer != null)
        {
            return await ConvertCodeWithTwoPhaseAsync(document, compilationUnit, analyzer, root);
        }

        // Fall back to the legacy rewriter-based approach
        return await ConvertCodeWithRewritersAsync(document, compilationUnit, semanticModel, compilation, root);
    }

    /// <summary>
    /// Creates a two-phase migration analyzer for this framework.
    /// Override in derived classes to enable the two-phase architecture.
    /// Returns null to use the legacy rewriter-based approach.
    /// </summary>
    protected virtual MigrationAnalyzer? CreateTwoPhaseAnalyzer(SemanticModel semanticModel, Compilation compilation)
    {
        return null;
    }

    /// <summary>
    /// New two-phase architecture: Analyze first (while semantic model valid), then transform (pure syntax).
    /// This avoids semantic model staleness issues that plague the rewriter-based approach.
    /// </summary>
    private async Task<Document> ConvertCodeWithTwoPhaseAsync(
        Document document,
        CompilationUnitSyntax compilationUnit,
        MigrationAnalyzer analyzer,
        SyntaxNode? originalRoot)
    {
        // Phase 1: Analyze - collect all conversion targets while semantic model is valid
        // Returns annotated root (nodes marked for conversion) and the conversion plan
        var (annotatedRoot, plan) = analyzer.Analyze(compilationUnit);

        // Phase 2: Transform - apply conversions using only syntax operations
        // Uses annotations to find nodes, no semantic model needed
        var transformer = new MigrationTransformer(plan, FrameworkName);
        var transformedRoot = transformer.Transform(annotatedRoot);

        // Phase 2.5: Apply framework-specific syntax-only transformations
        // These use CSharpSyntaxRewriter but don't need the semantic model
        transformedRoot = ApplyTwoPhasePostTransformations(transformedRoot);

        // Phase 2.6: Re-check usings after post-transformations
        // Post-transformations like NUnitExpectedResultRewriter may introduce async code
        // that wasn't present during the initial usings transformation
        transformedRoot = MigrationHelpers.AddTUnitUsings(transformedRoot);

        // Final cleanup (pure syntax operations)
        transformedRoot = CleanupClassMemberLeadingTrivia(transformedRoot);
        transformedRoot = CleanupEndOfFileTrivia(transformedRoot);
        transformedRoot = NormalizeLineEndings(transformedRoot, originalRoot!);

        // Add TODO comments for any failures
        if (plan.HasFailures)
        {
            transformedRoot = AddTwoPhaseFailureTodoComments(transformedRoot, plan);
        }

        return document.WithSyntaxRoot(transformedRoot);
    }

    /// <summary>
    /// Apply framework-specific post-transformations after the main two-phase processing.
    /// These should be pure syntax operations that don't require the semantic model.
    /// Override in derived classes to add framework-specific transformations.
    /// </summary>
    protected virtual CompilationUnitSyntax ApplyTwoPhasePostTransformations(CompilationUnitSyntax compilationUnit)
    {
        return compilationUnit;
    }

    /// <summary>
    /// Legacy rewriter-based approach for frameworks that haven't migrated to two-phase yet.
    /// </summary>
    private async Task<Document> ConvertCodeWithRewritersAsync(
        Document document,
        CompilationUnitSyntax compilationUnit,
        SemanticModel semanticModel,
        Compilation compilation,
        SyntaxNode? root)
    {
        var context = new MigrationContext();

        // Step 1: Collect interface-implementing methods BEFORE any syntax modifications
        // while the semantic model is still valid for the original syntax tree
        var interfaceImplementingMethods = TryCollectInterfaceMethods(
            compilationUnit, semanticModel, context);

        // Step 2: Convert assertions FIRST (while semantic model still matches the syntax tree)
        compilationUnit = TryApplyRewriter(
            compilationUnit,
            () => CreateAssertionRewriter(semanticModel, compilation),
            context,
            "AssertionConversion");

        // Step 3: Framework-specific conversions (also use semantic model while it still matches)
        compilationUnit = TryApplyFrameworkSpecific(
            compilationUnit, semanticModel, compilation, context);

        // Step 4: Fix method signatures that now contain await but aren't marked async
        // Pass the collected interface methods to avoid converting interface implementations
        compilationUnit = TryApplyRewriter(
            compilationUnit,
            () => new AsyncMethodSignatureRewriter(interfaceImplementingMethods),
            context,
            "AsyncSignatureFix");

        // Step 5: Remove unnecessary base classes and interfaces
        compilationUnit = TryApplyRewriter(
            compilationUnit,
            () => CreateBaseTypeRewriter(semanticModel, compilation),
            context,
            "BaseTypeRemoval");

        // Step 6: Update lifecycle methods
        compilationUnit = TryApplyRewriter(
            compilationUnit,
            () => CreateLifecycleRewriter(compilation),
            context,
            "LifecycleConversion");

        // Step 7: Convert attributes
        compilationUnit = TryApplyRewriter(
            compilationUnit,
            () => CreateAttributeRewriter(compilation),
            context,
            "AttributeConversion");

        // Step 8: Ensure [Test] attribute is present when data attributes exist (NUnit-specific)
        if (ShouldEnsureTestAttribute())
        {
            compilationUnit = TryApplyRewriter(
                compilationUnit,
                () => new TestAttributeEnsurer(),
                context,
                "TestAttributeEnsurer");
        }

        // Step 9: Remove framework usings and add TUnit usings (do this LAST)
        // These are pure syntax operations with minimal risk
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

        // Step 10: Clean up trivia issues that can occur after transformations
        compilationUnit = CleanupClassMemberLeadingTrivia(compilationUnit);
        compilationUnit = CleanupEndOfFileTrivia(compilationUnit);

        // Normalize line endings to match original document (fixes cross-platform issues)
        compilationUnit = NormalizeLineEndings(compilationUnit, root);

        // Add TODO comments for any failures so users know what needs manual attention
        if (context.HasFailures)
        {
            compilationUnit = AddFailureTodoComments(compilationUnit, context);
        }

        // Return the document with updated syntax root, preserving original formatting
        return document.WithSyntaxRoot(compilationUnit);
    }

    /// <summary>
    /// Safely collects interface-implementing methods before any syntax modifications.
    /// </summary>
    private static HashSet<string> TryCollectInterfaceMethods(
        CompilationUnitSyntax root,
        SemanticModel semanticModel,
        MigrationContext context)
    {
        try
        {
            return AsyncMethodSignatureRewriter.CollectInterfaceImplementingMethods(root, semanticModel);
        }
        catch (Exception ex)
        {
            context.RecordFailure("CollectInterfaceMethods", ex);
            return new HashSet<string>();
        }
    }

    /// <summary>
    /// Safely applies a syntax rewriter, recording any failures to the migration context.
    /// </summary>
    private static CompilationUnitSyntax TryApplyRewriter(
        CompilationUnitSyntax root,
        Func<CSharpSyntaxRewriter> rewriterFactory,
        MigrationContext context,
        string stepName)
    {
        try
        {
            var rewriter = rewriterFactory();
            return (CompilationUnitSyntax)rewriter.Visit(root);
        }
        catch (Exception ex)
        {
            context.RecordFailure(stepName, ex);
            return root; // Return unchanged, continue with other steps
        }
    }

    /// <summary>
    /// Safely applies framework-specific conversions, recording any failures.
    /// </summary>
    private CompilationUnitSyntax TryApplyFrameworkSpecific(
        CompilationUnitSyntax root,
        SemanticModel semanticModel,
        Compilation compilation,
        MigrationContext context)
    {
        try
        {
            return ApplyFrameworkSpecificConversions(root, semanticModel, compilation);
        }
        catch (Exception ex)
        {
            context.RecordFailure("FrameworkSpecificConversions", ex);
            return root;
        }
    }

    /// <summary>
    /// Adds TODO comments at the top of the file summarizing migration failures.
    /// This helps users identify what needs manual attention.
    /// </summary>
    private static CompilationUnitSyntax AddFailureTodoComments(
        CompilationUnitSyntax root,
        MigrationContext context)
    {
        // Group failures by step and create summary comments
        var failureSummary = context.Failures
            .GroupBy(f => f.Step)
            .Select(g => $"// TODO: TUnit migration - {g.Key}: {g.Count()} item(s) could not be converted automatically")
            .ToList();

        if (failureSummary.Count == 0)
        {
            return root;
        }

        // Add header comment
        var commentTrivia = new List<SyntaxTrivia>
        {
            SyntaxFactory.Comment("// ============================================================"),
            SyntaxFactory.EndOfLine("\n"),
            SyntaxFactory.Comment("// TUnit Migration: Some items require manual attention"),
            SyntaxFactory.EndOfLine("\n")
        };

        // Add failure summary lines
        foreach (var summary in failureSummary)
        {
            commentTrivia.Add(SyntaxFactory.Comment(summary));
            commentTrivia.Add(SyntaxFactory.EndOfLine("\n"));
        }

        commentTrivia.Add(SyntaxFactory.Comment("// ============================================================"));
        commentTrivia.Add(SyntaxFactory.EndOfLine("\n"));
        commentTrivia.Add(SyntaxFactory.EndOfLine("\n"));

        var existingTrivia = root.GetLeadingTrivia();
        return root.WithLeadingTrivia(SyntaxFactory.TriviaList(commentTrivia).AddRange(existingTrivia));
    }

    /// <summary>
    /// Adds TODO comments for failures from the two-phase architecture's ConversionPlan.
    /// </summary>
    private static CompilationUnitSyntax AddTwoPhaseFailureTodoComments(
        CompilationUnitSyntax root,
        ConversionPlan plan)
    {
        var failureSummary = plan.Failures
            .GroupBy(f => f.Phase)
            .Select(g => $"// TODO: TUnit migration - {g.Key}: {g.Count()} item(s) could not be converted automatically")
            .ToList();

        if (failureSummary.Count == 0)
        {
            return root;
        }

        var commentTrivia = new List<SyntaxTrivia>
        {
            SyntaxFactory.Comment("// ============================================================"),
            SyntaxFactory.EndOfLine("\n"),
            SyntaxFactory.Comment("// TUnit Migration: Some items require manual attention"),
            SyntaxFactory.EndOfLine("\n")
        };

        foreach (var summary in failureSummary)
        {
            commentTrivia.Add(SyntaxFactory.Comment(summary));
            commentTrivia.Add(SyntaxFactory.EndOfLine("\n"));
        }

        commentTrivia.Add(SyntaxFactory.Comment("// ============================================================"));
        commentTrivia.Add(SyntaxFactory.EndOfLine("\n"));
        commentTrivia.Add(SyntaxFactory.EndOfLine("\n"));

        var existingTrivia = root.GetLeadingTrivia();
        return root.WithLeadingTrivia(SyntaxFactory.TriviaList(commentTrivia).AddRange(existingTrivia));
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
    /// Preserves preprocessor directives (#region, #if, #endif, etc.) and associated comments.
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

            // Build the new trivia list, preserving preprocessor directives and their context
            var triviaToKeep = new List<SyntaxTrivia>();
            var consecutiveNewlines = 0;
            var lastWasPreprocessorOrComment = false;

            foreach (var trivia in leadingTrivia)
            {
                // Always preserve preprocessor directives
                if (IsPreprocessorDirective(trivia))
                {
                    triviaToKeep.Add(trivia);
                    consecutiveNewlines = 0;
                    lastWasPreprocessorOrComment = true;
                    continue;
                }

                // Always preserve comments
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    triviaToKeep.Add(trivia);
                    consecutiveNewlines = 0;
                    lastWasPreprocessorOrComment = true;
                    continue;
                }

                // Preserve whitespace (indentation)
                if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    triviaToKeep.Add(trivia);
                    continue;
                }

                // Handle newlines: allow one newline after preprocessor/comment, remove excessive newlines
                if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    consecutiveNewlines++;

                    // Keep the first newline after a preprocessor directive or comment
                    // This ensures proper formatting like:
                    //   #region Test
                    //   [Test]
                    if (lastWasPreprocessorOrComment && consecutiveNewlines == 1)
                    {
                        triviaToKeep.Add(trivia);
                    }
                    // Otherwise skip excessive newlines at the start
                    lastWasPreprocessorOrComment = false;
                    continue;
                }

                // Preserve any other trivia (structured trivia, etc.)
                triviaToKeep.Add(trivia);
                lastWasPreprocessorOrComment = false;
            }

            var newFirstMember = firstMember.WithLeadingTrivia(triviaToKeep);
            var updatedClass = classToFix.ReplaceNode(firstMember, newFirstMember);
            currentRoot = currentRoot.ReplaceNode(classToFix, updatedClass);
        }

        return currentRoot;
    }

    /// <summary>
    /// Checks if a trivia is a preprocessor directive.
    /// </summary>
    private static bool IsPreprocessorDirective(SyntaxTrivia trivia)
    {
        return trivia.IsKind(SyntaxKind.RegionDirectiveTrivia) ||
               trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia) ||
               trivia.IsKind(SyntaxKind.IfDirectiveTrivia) ||
               trivia.IsKind(SyntaxKind.ElseDirectiveTrivia) ||
               trivia.IsKind(SyntaxKind.ElifDirectiveTrivia) ||
               trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia) ||
               trivia.IsKind(SyntaxKind.DefineDirectiveTrivia) ||
               trivia.IsKind(SyntaxKind.UndefDirectiveTrivia) ||
               trivia.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia) ||
               trivia.IsKind(SyntaxKind.PragmaChecksumDirectiveTrivia) ||
               trivia.IsKind(SyntaxKind.NullableDirectiveTrivia);
    }

    /// <summary>
    /// Finds a class with excessive leading trivia on its first member.
    /// Returns null if no such class exists.
    /// Only considers trivia "excessive" if there are multiple consecutive newlines
    /// without preprocessor directives or comments between them.
    /// </summary>
    private static ClassDeclarationSyntax? FindClassWithExcessiveLeadingTrivia(CompilationUnitSyntax root)
    {
        return root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => c.Members.Any())
            .FirstOrDefault(c =>
            {
                var leadingTrivia = c.Members.First().GetLeadingTrivia();

                // Check for excessive newlines (more than one consecutive newline without meaningful trivia)
                var consecutiveNewlines = 0;
                foreach (var trivia in leadingTrivia)
                {
                    if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        consecutiveNewlines++;
                        if (consecutiveNewlines > 1)
                        {
                            return true; // Excessive newlines found
                        }
                    }
                    else if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                    {
                        // Non-whitespace, non-newline trivia resets the counter
                        consecutiveNewlines = 0;
                    }
                }

                return false;
            });
    }

    /// <summary>
    /// Removes trailing blank lines at end of file.
    /// Files should end immediately after the closing brace with no trailing newlines.
    /// Preserves a newline when the EndOfFileToken has leading preprocessor directives (e.g., #endif).
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
                // Check if the EndOfFileToken has leading trivia with preprocessor directives
                // (like #endif) or other meaningful content. If so, preserve one trailing newline
                // to separate the last member from the directive.
                var endOfFileLeadingTrivia = compilationUnit.EndOfFileToken.LeadingTrivia;
                var hasContentAfterLastMember = endOfFileLeadingTrivia.Any(t =>
                    !t.IsKind(SyntaxKind.EndOfLineTrivia) &&
                    !t.IsKind(SyntaxKind.WhitespaceTrivia));

                var newTrivia = trailingTrivia
                    .Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia))
                    .ToList();

                if (hasContentAfterLastMember)
                {
                    newTrivia.Add(SyntaxFactory.EndOfLine("\n"));
                }

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