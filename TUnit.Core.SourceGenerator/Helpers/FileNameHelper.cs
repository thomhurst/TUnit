using System.Text;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Helpers;

/// <summary>
/// Helper for generating deterministic, sanitized filenames for generated source files.
/// </summary>
internal static class FileNameHelper
{
    /// <summary>
    /// Generates a deterministic filename for a test method.
    /// Format: {Namespace}_{ClassName}_{MethodName}__{ParameterTypes}.g.cs
    /// </summary>
    /// <param name="typeSymbol">The type symbol for the test class</param>
    /// <param name="methodSymbol">The method symbol for the test method</param>
    /// <returns>A deterministic filename like "MyNamespace_MyClass_MyMethod__Int32_String.g.cs"</returns>
    public static string GetDeterministicFileNameForMethod(INamedTypeSymbol typeSymbol, IMethodSymbol methodSymbol)
    {
        var sb = new StringBuilder();

        // Add namespace
        if (!typeSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            sb.Append(SanitizeForFileName(typeSymbol.ContainingNamespace.ToDisplayString()));
            sb.Append('_');
        }

        // Add all containing types (outer classes first, then inner classes)
        var containingTypes = new List<string>();
        var currentType = typeSymbol;
        while (currentType != null)
        {
            containingTypes.Add(SanitizeForFileName(currentType.Name));
            currentType = currentType.ContainingType;
        }

        // Reverse to get outer-to-inner order
        containingTypes.Reverse();

        // Append containing types from outer to inner
        for (int i = 0; i < containingTypes.Count; i++)
        {
            if (i > 0) sb.Append('_');
            sb.Append(containingTypes[i]);
        }

        // Add generic parameters if any (for the innermost type)
        if (typeSymbol.TypeArguments.Length > 0)
        {
            sb.Append('_');
            for (int i = 0; i < typeSymbol.TypeArguments.Length; i++)
            {
                if (i > 0) sb.Append('_');
                sb.Append(SanitizeForFileName(typeSymbol.TypeArguments[i].Name));
            }
        }
        sb.Append('_');

        // Add method name
        sb.Append(SanitizeForFileName(methodSymbol.Name));

        // Add parameters with double underscore separator
        if (methodSymbol.Parameters.Length > 0)
        {
            sb.Append("__");
            for (int i = 0; i < methodSymbol.Parameters.Length; i++)
            {
                if (i > 0) sb.Append('_');
                sb.Append(SanitizeForFileName(methodSymbol.Parameters[i].Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
        }

        sb.Append(".g.cs");
        return sb.ToString();
    }

    /// <summary>
    /// Sanitizes a string to be safe for use in a filename.
    /// Replaces invalid characters with underscores.
    /// </summary>
    private static string SanitizeForFileName(string input)
    {
        return string.Create(input.Length, input, (span, input) =>
        {
            var index = 0;
            foreach (var c in input)
            {
                // Replace invalid filename characters and special type characters with underscore
                if (c == '<' || c == '>' || c == ':' || c == '"' || c == '/' || c == '\\' ||
                    c == '|' || c == '?' || c == '*' || c == '.' || c == ',' || c == ' ' ||
                    c == '(' || c == ')' || c == '[' || c == ']' || c == '{' || c == '}')
                {
                    span[index] = '_';
                }
                else
                {
                    span[index] = c;
                }

                index++;
            }
        });
    }

}
