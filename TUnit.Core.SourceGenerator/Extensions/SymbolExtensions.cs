using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class SymbolExtensions
{
    public static bool IsConst(this ISymbol? symbol, out object? constantValue)
    {
        if (symbol is null)
        {
            constantValue = null;
            return false;
        }

        if (symbol is IFieldSymbol fieldSymbol)
        {
            constantValue = fieldSymbol.ConstantValue;
            return fieldSymbol.IsConst;
        }

        if (symbol is ILocalSymbol localSymbol)
        {
            constantValue = localSymbol.ConstantValue;
            return localSymbol.IsConst;
        }

        constantValue = null;
        return false;
    }
    
    /// <summary>
    /// Creates an IEqualityComparer for tuples that uses SymbolEqualityComparer for symbol comparison
    /// </summary>
    public static IEqualityComparer<(INamedTypeSymbol, string)> ToTupleComparer(this IEqualityComparer<ISymbol> comparer)
    {
        return new TupleSymbolComparer(comparer);
    }
    
    private class TupleSymbolComparer : IEqualityComparer<(INamedTypeSymbol, string)>
    {
        private readonly IEqualityComparer<ISymbol> _symbolComparer;
        
        public TupleSymbolComparer(IEqualityComparer<ISymbol> symbolComparer)
        {
            _symbolComparer = symbolComparer;
        }
        
        public bool Equals((INamedTypeSymbol, string) x, (INamedTypeSymbol, string) y)
        {
            return _symbolComparer.Equals(x.Item1, y.Item1) && x.Item2 == y.Item2;
        }
        
        public int GetHashCode((INamedTypeSymbol, string) obj)
        {
            var hash1 = _symbolComparer.GetHashCode(obj.Item1);
            var hash2 = obj.Item2?.GetHashCode() ?? 0;
            return hash1 ^ hash2;
        }
    }
}
