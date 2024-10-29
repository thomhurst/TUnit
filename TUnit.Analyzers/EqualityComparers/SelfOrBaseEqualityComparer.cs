using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.EqualityComparers;

public class SelfOrBaseEqualityComparer(Compilation compilation) : EqualityComparer<ITypeSymbol?>
{
    public override bool Equals(ITypeSymbol? superType, ITypeSymbol? subType)
    {
        if (compilation.HasImplicitConversion(subType, superType))
        {
            return true;
        }

        if (superType is INamedTypeSymbol { IsGenericType: true, TypeArguments: [{ TypeKind: TypeKind.TypeParameter }] } namedType)
        {
            // `IEnumerable<>`
            if (subType is IArrayTypeSymbol { ElementType: { } elementType })
            {
                var specializedSuper = namedType.OriginalDefinition.Construct(elementType);
                return compilation.HasImplicitConversion(subType, specializedSuper);
            }

            if (subType is INamedTypeSymbol { IsGenericType: true, TypeArguments: [{ } genericArgument] })
            {
                var specializedSuper = namedType.OriginalDefinition.Construct(genericArgument);
                return compilation.HasImplicitConversion(subType, specializedSuper);
            }
        }
        else if (superType is IArrayTypeSymbol { ElementType.Kind: SymbolKind.TypeParameter })
        {
            // `T[]`
            return true;
        }

        return false;
    }

    public override int GetHashCode(ITypeSymbol? obj)
    {
        return SymbolEqualityComparer.Default.GetHashCode(obj);
    }
}