using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Helpers;

/// <summary>
/// Helper for generating deterministic, sanitized filenames for generated source files.
/// </summary>
internal static class FileNameHelper
{
    /// <summary>
    /// Generates a deterministic filename for a test class.
    /// Uses the fully qualified type name, sanitized for filesystem compatibility,
    /// with a short stable hash to prevent collisions and path length issues.
    /// </summary>
    /// <param name="typeSymbol">The type symbol for the test class</param>
    /// <returns>A deterministic filename like "MyNamespace_MyClass_A1B2C3D4.g.cs"</returns>
    public static string GetDeterministicFileName(INamedTypeSymbol typeSymbol)
    {
        // Get the fully qualified name (e.g., "MyNamespace.SubNamespace.MyClass<T>")
        var fullyQualifiedName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Remove the "global::" prefix if present
        if (fullyQualifiedName.StartsWith("global::"))
        {
            fullyQualifiedName = fullyQualifiedName.Substring("global::".Length);
        }

        // Sanitize for filename
        var sanitized = SanitizeForFileName(fullyQualifiedName);

        // Add a short stable hash to prevent collisions and handle path length limits
        var hash = GetStableHash(fullyQualifiedName);

        return $"{sanitized}_{hash}.g.cs";
    }

    /// <summary>
    /// Generates a deterministic filename for a test method.
    /// Uses the class and method names with parameter signature to ensure uniqueness.
    /// </summary>
    /// <param name="typeSymbol">The type symbol for the test class</param>
    /// <param name="methodSymbol">The method symbol for the test method</param>
    /// <returns>A deterministic filename like "MyClass_MyMethod_ParamTypes_A1B2C3D4.g.cs"</returns>
    public static string GetDeterministicFileNameForMethod(INamedTypeSymbol typeSymbol, IMethodSymbol methodSymbol)
    {
        // Build a unique string combining class and method information
        var typeFullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (typeFullName.StartsWith("global::"))
        {
            typeFullName = typeFullName.Substring("global::".Length);
        }

        var methodFullName = methodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (methodFullName.StartsWith("global::"))
        {
            methodFullName = methodFullName.Substring("global::".Length);
        }

        // Combine for uniqueness (includes parameter types, generics, etc.)
        var combined = $"{typeFullName}::{methodFullName}";

        // Sanitize the class and method names separately for readability
        var className = SanitizeForFileName(typeSymbol.Name);
        var methodName = SanitizeForFileName(methodSymbol.Name);

        // Generate hash from the full signature to ensure uniqueness
        var hash = GetStableHash(combined);

        return $"{className}_{methodName}_{hash}.g.cs";
    }

    /// <summary>
    /// Sanitizes a string to be safe for use in a filename.
    /// Replaces invalid characters with underscores.
    /// </summary>
    private static string SanitizeForFileName(string input)
    {
        var sb = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            // Replace invalid filename characters with underscore
            if (c == '<' || c == '>' || c == ':' || c == '"' || c == '/' || c == '\\' ||
                c == '|' || c == '?' || c == '*' || c == '.' || c == ',' || c == ' ')
            {
                sb.Append('_');
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a stable 8-character hash from the input string.
    /// Same input always produces the same hash (deterministic).
    /// </summary>
    public static string GetStableHash(string input)
    {
        using var sha1 = SHA1.Create();
        var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Take first 4 bytes and convert to hex (8 characters)
        var sb = new StringBuilder(8);
        for (int i = 0; i < 4 && i < hashBytes.Length; i++)
        {
            sb.Append(hashBytes[i].ToString("X2"));
        }

        return sb.ToString();
    }
}
