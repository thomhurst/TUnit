using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.CodeFixers.Base;
using TUnit.Analyzers.CodeFixers.Base.TwoPhase;
using TUnit.Analyzers.CodeFixers.TwoPhase;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XUnitMigrationCodeFixProvider)), Shared]
public class XUnitMigrationCodeFixProvider : BaseMigrationCodeFixProvider
{
    protected override string FrameworkName => "XUnit";
    protected override string DiagnosticId => Rules.XunitMigration.Id;
    protected override string CodeFixTitle => Rules.XunitMigration.Title.ToString();

    protected override bool ShouldAddTUnitUsings() => true;

    protected override MigrationAnalyzer? CreateTwoPhaseAnalyzer(SemanticModel semanticModel, Compilation compilation)
    {
        return new XUnitTwoPhaseAnalyzer(semanticModel, compilation);
    }

    // The following methods are required by the base class but are only used in the legacy
    // rewriter-based approach. Since XUnit always uses the two-phase architecture (above),
    // these implementations are never called - they exist only to satisfy the abstract contract.

    protected override AttributeRewriter CreateAttributeRewriter(Compilation compilation)
    {
        return new PassThroughAttributeRewriter();
    }

    protected override CSharpSyntaxRewriter CreateAssertionRewriter(SemanticModel semanticModel, Compilation compilation)
    {
        return new PassThroughRewriter();
    }

    protected override CSharpSyntaxRewriter CreateBaseTypeRewriter(SemanticModel semanticModel, Compilation compilation)
    {
        return new PassThroughRewriter();
    }

    protected override CSharpSyntaxRewriter CreateLifecycleRewriter(Compilation compilation)
    {
        return new PassThroughRewriter();
    }

    protected override CompilationUnitSyntax ApplyFrameworkSpecificConversions(
        CompilationUnitSyntax compilationUnit,
        SemanticModel semanticModel,
        Compilation compilation)
    {
        // Not used - two-phase architecture handles all conversions
        return compilationUnit;
    }

    /// <summary>
    /// Pass-through rewriter that makes no changes.
    /// Used to satisfy abstract method contracts when two-phase architecture is active.
    /// </summary>
    private class PassThroughRewriter : CSharpSyntaxRewriter
    {
    }

    /// <summary>
    /// Pass-through attribute rewriter that makes no changes.
    /// Used to satisfy abstract method contracts when two-phase architecture is active.
    /// </summary>
    private class PassThroughAttributeRewriter : AttributeRewriter
    {
        protected override string FrameworkName => "XUnit";

        protected override bool IsFrameworkAttribute(string attributeName)
        {
            return false;
        }

        protected override AttributeArgumentListSyntax? ConvertAttributeArguments(
            AttributeArgumentListSyntax argumentList,
            string attributeName)
        {
            return argumentList;
        }
    }
}
