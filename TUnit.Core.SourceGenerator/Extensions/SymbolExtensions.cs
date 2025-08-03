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
}
