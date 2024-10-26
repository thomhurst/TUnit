using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.EqualityComparers;

public class SelfOrBaseEqualityComparer(Compilation compilation) : EqualityComparer<ITypeSymbol?>
{
    public override bool Equals(ITypeSymbol? superType, ITypeSymbol? subType)
    {
        return compilation.HasImplicitConversion(subType, superType);
    }

    public override int GetHashCode(ITypeSymbol? obj)
    {
        return SymbolEqualityComparer.Default.GetHashCode(obj);
    }
}