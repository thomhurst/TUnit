using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

/// <summary>
/// Helper class for detecting and extracting extern alias information from source code.
/// </summary>
public static class ExternAliasHelper
{
    /// <summary>
    /// Checks if any syntax tree in the compilation has extern alias directives.
    /// This is a fast check suitable for early detection.
    /// </summary>
    public static bool HasExternAliases(Compilation compilation)
    {
        return compilation.SyntaxTrees.Any(HasExternAliases);
    }

    /// <summary>
    /// Checks if a specific syntax tree has extern alias directives.
    /// </summary>
    public static bool HasExternAliases(SyntaxTree tree)
    {
        // Very fast - only check direct children of the compilation unit
        var root = tree.GetRoot();
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return false;
        }

        return compilationUnit.Externs.Any();
    }

    /// <summary>
    /// Extracts extern alias context from a syntax node (class, method, etc.).
    /// Returns Empty context if no extern aliases are found.
    /// </summary>
    public static ExternAliasContext ExtractContext(SyntaxNode node, SemanticModel semanticModel)
    {
        var syntaxTree = node.SyntaxTree;
        if (!HasExternAliases(syntaxTree))
        {
            return ExternAliasContext.Empty;
        }

        var root = syntaxTree.GetRoot();
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return ExternAliasContext.Empty;
        }

        // Get all extern alias directives
        var externAliases = compilationUnit.Externs
            .Select(e => e.Identifier.ValueText)
            .ToImmutableArray();

        if (externAliases.IsEmpty)
        {
            return ExternAliasContext.Empty;
        }

        // Build namespace to alias mapping from using directives
        var namespaceToAlias = ImmutableDictionary.CreateBuilder<string, string>();

        foreach (var usingDirective in compilationUnit.Usings)
        {
            NameSyntax? nameToCheck = null;

            // Handle both:
            // 1. using ExternAlias::Namespace.SubNamespace;
            // 2. using TypeAlias = ExternAlias::Namespace.Type;
            if (usingDirective.Alias != null)
            {
                // This is a using alias like: using Alias = Namespace.Type;
                // Check if the right side is alias-qualified
                nameToCheck = usingDirective.Name;
            }
            else
            {
                // Regular using directive
                nameToCheck = usingDirective.Name;
            }

            if (nameToCheck == null)
            {
                continue;
            }

            // Check if the using directive uses an extern alias qualifier
            // Format: using ExternAlias::Namespace.SubNamespace;
            // Or: using T = ExternAlias::Namespace.Type;
            if (nameToCheck is AliasQualifiedNameSyntax aliasQualifiedName)
            {
                var alias = aliasQualifiedName.Alias.Identifier.ValueText;

                if (externAliases.Contains(alias))
                {
                    // Get the qualified name string (e.g., "Newtonsoft.Json.Linq.JObject")
                    var qualifiedName = aliasQualifiedName.Name.ToString();

                    // Split and create namespace mappings for all namespace levels
                    // For "Newtonsoft.Json.Linq.JObject" we create mappings for:
                    // "Newtonsoft", "Newtonsoft.Json", "Newtonsoft.Json.Linq"
                    // (but NOT the last part if it looks like a type name)
                    var parts = qualifiedName.Split('.');

                    // Map all parts except potentially the last one
                    for (int i = 0; i < parts.Length; i++)
                    {
                        var ns = string.Join(".", parts.Take(i + 1));

                        // Special handling for the last part - only add if it's all lowercase or mixed
                        // to avoid mapping type names like "JObject"
                        if (i == parts.Length - 1)
                        {
                            // Check if it looks like a type (PascalCase starting with uppercase)
                            // If the first character is uppercase AND there's a lowercase character,
                            // it's likely a type name, so we map it but also map parent namespace
                            var lastPart = parts[i];
                            if (char.IsUpper(lastPart[0]) && i > 0)
                            {
                                // Map the parent namespace (without the type name)
                                var parentNs = string.Join(".", parts.Take(i));
                                if (!namespaceToAlias.ContainsKey(parentNs))
                                {
                                    namespaceToAlias[parentNs] = alias;
                                }
                            }
                        }

                        // Always map this level
                        if (!namespaceToAlias.ContainsKey(ns))
                        {
                            namespaceToAlias[ns] = alias;
                        }
                    }
                }
            }
        }

        return new ExternAliasContext(externAliases, namespaceToAlias.ToImmutable());
    }

    /// <summary>
    /// Extracts extern alias directives as source code strings for inclusion in generated files.
    /// Returns lines like "extern alias DirectDependency;"
    /// </summary>
    public static ImmutableArray<string> GetExternAliasDirectives(ExternAliasContext context)
    {
        if (!context.HasExternAliases)
        {
            return ImmutableArray<string>.Empty;
        }

        return context.ExternAliases
            .Select(alias => $"extern alias {alias};")
            .ToImmutableArray();
    }
}
