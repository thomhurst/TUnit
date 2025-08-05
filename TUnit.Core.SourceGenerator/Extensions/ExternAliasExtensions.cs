using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

internal static class ExternAliasExtensions
{
    /// <summary>
    /// Gets the extern alias for a type symbol if it exists in the compilation context
    /// </summary>
    public static string? GetExternAlias(this ITypeSymbol typeSymbol, Compilation compilation)
    {
        if (typeSymbol.ContainingAssembly == null)
        {
            return null;
        }

        // Check each metadata reference to see if it has an alias and matches our assembly
        foreach (var reference in compilation.References)
        {
            if (reference is MetadataReference metadataRef)
            {
                // Get the assembly symbol for this reference
                var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                
                // Check if this assembly matches our type's containing assembly
                if (assemblySymbol != null && 
                    SymbolEqualityComparer.Default.Equals(assemblySymbol, typeSymbol.ContainingAssembly))
                {
                    // Check if this reference has aliases
                    if (metadataRef.Properties.Aliases != null && metadataRef.Properties.Aliases.Length > 0)
                    {
                        // Return the first non-global alias
                        foreach (var alias in metadataRef.Properties.Aliases)
                        {
                            if (!string.IsNullOrEmpty(alias) && alias != MetadataReferenceProperties.GlobalAlias)
                            {
                                return alias;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the type name with extern alias prefix if applicable
    /// </summary>
    public static string ToDisplayStringWithAlias(this ITypeSymbol typeSymbol, Compilation compilation, SymbolDisplayFormat format)
    {
        var alias = typeSymbol.GetExternAlias(compilation);
        var displayString = typeSymbol.ToDisplayString(format);
        
        if (!string.IsNullOrEmpty(alias))
        {
            // Replace the global:: prefix with the alias
            if (displayString.StartsWith("global::"))
            {
                return alias + "::" + displayString.Substring("global::".Length);
            }
            else if (!displayString.Contains("::"))
            {
                // Add the alias prefix if no namespace qualifier exists
                return alias + "::" + displayString;
            }
        }

        return displayString;
    }
}