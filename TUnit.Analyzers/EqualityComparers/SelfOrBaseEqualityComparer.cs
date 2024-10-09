using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.EqualityComparers;

public class SelfOrBaseEqualityComparer : EqualityComparer<ITypeSymbol?>
{
    public static SelfOrBaseEqualityComparer Instance { get; } = new();
    
    private SelfOrBaseEqualityComparer()
    {
    }
    
    public override bool Equals(ITypeSymbol? x, ITypeSymbol? y)
    {
        var type = x;
        
        while (type != null)
        {
            if (SymbolEqualityComparer.Default.Equals(type, y))
            {
                return true;
            }
            
            type = type.BaseType;
        }

        return false;
    }

    public override int GetHashCode(ITypeSymbol? obj)
    {
        return SymbolEqualityComparer.Default.GetHashCode(obj);
    }
}