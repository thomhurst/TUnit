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
    // Conservative limit to avoid PathTooLongException on Windows with net472,
    // which enforces the legacy 260-character MAX_PATH limit in Roslyn's AddSource.
    // Roslyn prepends the CWD to the hint name when calling Path.GetFullPathInternal,
    // so we must leave ~110 chars of headroom for the working directory path.
    private const int MaxHintNameLength = 150;

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

        var baseName = sb.ToString();

        // Truncate and append a hash if the name would exceed the limit
        const int suffixLength = 5; // ".g.cs"
        if (baseName.Length + suffixLength > MaxHintNameLength)
        {
            var hashSuffix = $"_{GetStableHashCode(baseName):X8}";
            var maxBaseLength = MaxHintNameLength - suffixLength - hashSuffix.Length;
            baseName = baseName.Substring(0, maxBaseLength) + hashSuffix;
        }

        return baseName + ".g.cs";
    }

    /// <summary>
    /// Computes a deterministic hash code for a string (FNV-1a).
    /// Unlike string.GetHashCode(), this is stable across processes and platforms.
    /// </summary>
    private static uint GetStableHashCode(string str)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var c in str)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return hash;
        }
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
