using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Helpers;

/// <summary>
/// Caches interface implementation checks to avoid repeated AllInterfaces traversals.
/// </summary>
/// <remarks>
/// Cache entries are keyed by the <see cref="ITypeSymbol"/> itself and held in a
/// <see cref="ConditionalWeakTable{TKey,TValue}"/>. This ties each entry's lifetime to the
/// symbol's lifetime, so the cache is reclaimed automatically when a <see cref="Compilation"/>
/// is collected. This avoids the cross-compilation symbol leak a long-lived static dictionary
/// would cause in extended IDE sessions.
/// </remarks>
public static class InterfaceCache
{
    /// <summary>
    /// Per-type cache of the globally-qualified names of every interface the type implements.
    /// </summary>
    private static readonly ConditionalWeakTable<ITypeSymbol, ImmutableHashSet<string>> InterfaceNames = new();

    private static ImmutableHashSet<string> GetInterfaceNames(ITypeSymbol type)
    {
        return InterfaceNames.GetValue(type, static t =>
        {
            var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
            foreach (var i in t.AllInterfaces)
            {
                builder.Add(i.GloballyQualified());
            }

            return builder.ToImmutable();
        });
    }

    /// <summary>
    /// Checks if a type implements a specific interface
    /// </summary>
    public static bool ImplementsInterface(ITypeSymbol type, string fullyQualifiedInterfaceName)
    {
        return GetInterfaceNames(type).Contains(fullyQualifiedInterfaceName);
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
