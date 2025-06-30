using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Utilities;

/// <summary>
/// Equality comparer for arrays of ITypeSymbol objects used in generic type resolution
/// Implements the exact pattern from the requirements specification
/// </summary>
internal sealed class TypeArrayComparer : IEqualityComparer<ITypeSymbol[]>
{
    public static readonly TypeArrayComparer Instance = new();

    private TypeArrayComparer() { }

    public bool Equals(ITypeSymbol[]? x, ITypeSymbol[]? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        if (x.Length != y.Length) return false;

        for (int i = 0; i < x.Length; i++)
        {
            if (!SymbolEqualityComparer.Default.Equals(x[i], y[i]))
                return false;
        }

        return true;
    }

    public int GetHashCode(ITypeSymbol[] obj)
    {
        // Use simple hash combining for .NET Standard 2.0 compatibility
        int hash = 17;
        foreach (var type in obj)
        {
            hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(type);
        }
        return hash;
    }
}