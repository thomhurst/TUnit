using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Helpers;

/// <summary>
/// Caches interface implementation checks to avoid repeated AllInterfaces traversals
/// </summary>
public static class InterfaceCache
{
    public static readonly ConcurrentDictionary<(ITypeSymbol Type, string InterfaceName), bool> _implementsCache = new(TypeStringTupleComparer.Default);
    private static readonly ConcurrentDictionary<(ITypeSymbol Type, string GenericInterfacePattern), INamedTypeSymbol?> _genericInterfaceCache = new(TypeStringTupleComparer.Default);

    /// <summary>
    /// Checks if a type implements a specific interface
    /// </summary>
    public static bool ImplementsInterface(ITypeSymbol type, string fullyQualifiedInterfaceName)
    {
        return _implementsCache.GetOrAdd((type, fullyQualifiedInterfaceName), key =>
            key.Type.AllInterfaces.Any(i => i.GloballyQualified() == key.InterfaceName));
    }

    /// <summary>
    /// Checks if a type implements a generic interface and returns the matching interface symbol
    /// </summary>
    public static INamedTypeSymbol? GetGenericInterface(ITypeSymbol type, string fullyQualifiedGenericPattern)
    {
        return _genericInterfaceCache.GetOrAdd((type, fullyQualifiedGenericPattern), key =>
            key.Type.AllInterfaces.FirstOrDefault(i =>
                i.IsGenericType &&
                i.ConstructedFrom.GloballyQualified() == key.GenericInterfacePattern));
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
        return _implementsCache.GetOrAdd((type, "System.Collections.Generic.IAsyncEnumerable<T>"), key =>
        {
            if (key.Type is INamedTypeSymbol { IsGenericType: true } namedType &&
                namedType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IAsyncEnumerable<T>")
            {
                return true;
            }

            return key.Type.AllInterfaces.Any(i =>
                i.IsGenericType &&
                i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IAsyncEnumerable<T>");
        });
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

        return _implementsCache.GetOrAdd((type, "System.Collections.IEnumerable"), key =>
            key.Type.AllInterfaces.Any(i =>
                i.OriginalDefinition.ToDisplayString() == "System.Collections.IEnumerable" ||
                (i.IsGenericType && i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")));
    }
}

internal sealed class TypeStringTupleComparer : IEqualityComparer<(ITypeSymbol Type, string Name)>
{
    public static readonly TypeStringTupleComparer Default = new();

    private TypeStringTupleComparer() { }

    public bool Equals((ITypeSymbol Type, string Name) x, (ITypeSymbol Type, string Name) y)
    {
        return Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.Equals(x.Type, y.Type) && x.Name == y.Name;
    }

    public int GetHashCode((ITypeSymbol Type, string Name) obj)
    {
        unchecked
        {
            return (Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.GetHashCode(obj.Type) * 397) ^ obj.Name.GetHashCode();
        }
    }
}
