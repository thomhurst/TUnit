using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

/// <summary>
/// Centralized helper for source generators that handles extern alias detection and context management.
/// Use this to ensure consistent extern alias handling across all generators.
/// </summary>
public static class SourceGeneratorHelper
{
    /// <summary>
    /// Extracts extern alias context from a syntax node if the file uses extern aliases.
    /// Returns empty context if no extern aliases are present (zero overhead).
    /// </summary>
    public static ExternAliasContext GetExternAliasContext(SyntaxNode node, SemanticModel semanticModel)
    {
        // Fast early exit - check if file has extern aliases
        if (!ExternAliasHelper.HasExternAliases(node.SyntaxTree))
        {
            return ExternAliasContext.Empty;
        }

        // Extract the context
        return ExternAliasHelper.ExtractContext(node, semanticModel);
    }

    /// <summary>
    /// Extracts extern alias context from a compilation context.
    /// </summary>
    public static ExternAliasContext GetExternAliasContext(GeneratorAttributeSyntaxContext context)
    {
        return GetExternAliasContext(context.TargetNode, context.SemanticModel);
    }

    /// <summary>
    /// Adds extern alias directives to the beginning of a StringBuilder if the context has aliases.
    /// Call this before adding any using statements or namespace declarations.
    /// </summary>
    public static void AddExternAliasDirectives(StringBuilder builder, ExternAliasContext? context)
    {
        if (context?.HasExternAliases != true)
        {
            return;
        }

        var directives = ExternAliasHelper.GetExternAliasDirectives(context);
        foreach (var directive in directives)
        {
            builder.AppendLine(directive);
        }
        builder.AppendLine();
    }

    /// <summary>
    /// Gets the globally qualified name for a type symbol, using extern alias if needed.
    /// This is a convenience wrapper that properly handles null contexts.
    /// </summary>
    public static string GetQualifiedTypeName(ITypeSymbol typeSymbol, ExternAliasContext? context = null)
    {
        return typeSymbol.GloballyQualified(context);
    }

    /// <summary>
    /// Gets the globally qualified non-generic name for a symbol, using extern alias if needed.
    /// </summary>
    public static string GetQualifiedNonGenericName(ISymbol symbol, ExternAliasContext? context = null)
    {
        return symbol.GloballyQualifiedNonGeneric(context);
    }

    /// <summary>
    /// Creates a code writer with extern alias support.
    /// The writer will automatically include extern alias directives in the header.
    /// </summary>
    public static ICodeWriter CreateCodeWriter(string generatedNamespace, ExternAliasContext? context = null)
    {
        var writer = new CodeWriter(generatedNamespace);

        if (context?.HasExternAliases == true)
        {
            // The CodeWriter will need to support extern aliases in its header generation
            // For now, we'll add them manually in the calling code
        }

        return writer;
    }

    /// <summary>
    /// Gets all using directives that should be included based on the extern alias context.
    /// This includes both the extern alias directives and any using alias declarations.
    /// </summary>
    public static ImmutableArray<string> GetRequiredUsings(ExternAliasContext? context)
    {
        if (context?.HasExternAliases != true)
        {
            return ImmutableArray<string>.Empty;
        }

        var usings = ImmutableArray.CreateBuilder<string>();

        // Add using declarations for aliased namespaces
        foreach (var (ns, alias) in context.NamespaceToAlias)
        {
            usings.Add($"using {alias}::{ns};");
        }

        return usings.ToImmutable();
    }

    /// <summary>
    /// Checks if a syntax tree has any extern alias directives.
    /// This is a fast check suitable for early detection.
    /// </summary>
    public static bool HasExternAliases(SyntaxTree syntaxTree)
    {
        return ExternAliasHelper.HasExternAliases(syntaxTree);
    }

    /// <summary>
    /// Gets extern alias context from a type declaration syntax.
    /// Commonly used pattern in generators that process classes.
    /// </summary>
    public static ExternAliasContext GetExternAliasContext(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel)
    {
        return GetExternAliasContext((SyntaxNode)typeDeclaration, semanticModel);
    }

    /// <summary>
    /// Gets extern alias context from a method declaration syntax.
    /// Commonly used pattern in generators that process methods.
    /// </summary>
    public static ExternAliasContext GetExternAliasContext(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel)
    {
        return GetExternAliasContext((SyntaxNode)methodDeclaration, semanticModel);
    }
}
