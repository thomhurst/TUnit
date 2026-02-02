using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Helpers;

/// <summary>
/// Caches interface implementation checks to avoid repeated AllInterfaces traversals
/// </summary>
public static class InterfaceCache
{
    /// <summary>
    /// Checks if a type implements a specific interface
    /// </summary>
    public static bool ImplementsInterface(ITypeSymbol type, string fullyQualifiedInterfaceName)
    {
        foreach (var i in type.AllInterfaces)
        {
            if (i.GloballyQualified() == fullyQualifiedInterfaceName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a type implements a generic interface and returns the matching interface symbol
    /// </summary>
    public static INamedTypeSymbol? GetGenericInterface(ITypeSymbol type, string fullyQualifiedGenericPattern)
    {
        foreach (var i in type.AllInterfaces)
        {
            if (i.IsGenericType && i.ConstructedFrom.GloballyQualified() == fullyQualifiedGenericPattern)
            {
                return i;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a type implements a generic interface
    /// </summary>
    public static bool ImplementsGenericInterface(ITypeSymbol type, string fullyQualifiedGenericPattern)
    {
        return GetGenericInterface(type, fullyQualifiedGenericPattern) != null;
    }

    /// <summary>
    /// Checks if a type implements IAsyncEnumerable&lt;T&gt;
    /// </summary>
    public static bool IsAsyncEnumerable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } namedType &&
            namedType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IAsyncEnumerable<T>")
        {
            return true;
        }

        return type.AllInterfaces.Any(i =>
            i.IsGenericType &&
            i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IAsyncEnumerable<T>");
    }

    /// <summary>
    /// Checks if a type implements IEnumerable (excluding string)
    /// </summary>
    public static bool IsEnumerable(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        return type.AllInterfaces.Any(i =>
        {
            var originalDefintion = i.OriginalDefinition.ToDisplayString();
            return originalDefintion == "System.Collections.IEnumerable" ||
                   (i.IsGenericType && originalDefintion ==
                       "System.Collections.Generic.IEnumerable<T>");
        });
    }
}
