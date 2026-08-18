using Microsoft.CodeAnalysis;

namespace TUnit.Mocks.SourceGenerator.Discovery;

internal static class TypeAccessibility
{
    /// <summary>
    /// Returns whether non-derived generated code in the consumer assembly can name
    /// <paramref name="type"/>. Constructor models are used by both the derived implementation
    /// and its separate factory, so protected access alone is insufficient.
    /// </summary>
    public static bool IsAccessibleFromAssembly(ITypeSymbol type, Compilation compilation)
    {
        switch (type)
        {
            case ITypeParameterSymbol:
                return true;

            case IPointerTypeSymbol or IFunctionPointerTypeSymbol:
                return false;

            case IArrayTypeSymbol array:
                return IsAccessibleFromAssembly(array.ElementType, compilation);

            case INamedTypeSymbol named:
                if (!compilation.IsSymbolAccessibleWithin(named, compilation.Assembly))
                {
                    return false;
                }

                if (named.ContainingType is not null
                    && !IsAccessibleFromAssembly(named.ContainingType, compilation))
                {
                    return false;
                }

                foreach (var typeArgument in named.TypeArguments)
                {
                    if (!IsAccessibleFromAssembly(typeArgument, compilation))
                    {
                        return false;
                    }
                }

                return true;

            default:
                return true;
        }
    }

    /// <summary>
    /// Returns whether every part of <paramref name="type"/> can appear in a public generated
    /// signature, including containing types, generic arguments, and array elements.
    /// </summary>
    public static bool IsEffectivelyPublic(ITypeSymbol type)
    {
        switch (type)
        {
            case ITypeParameterSymbol:
                return true;

            case IArrayTypeSymbol array:
                return IsEffectivelyPublic(array.ElementType);

            case INamedTypeSymbol named:
                if (named.DeclaredAccessibility != Accessibility.Public)
                {
                    return false;
                }

                if (named.ContainingType is not null && !IsEffectivelyPublic(named.ContainingType))
                {
                    return false;
                }

                foreach (var typeArgument in named.TypeArguments)
                {
                    if (!IsEffectivelyPublic(typeArgument))
                    {
                        return false;
                    }
                }

                return true;

            default:
                return true;
        }
    }
}
