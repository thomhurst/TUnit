using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Extensions;

public static class CompilationExtensions
{
    public static bool HasImplicitConversionOrGenericParameter(this Compilation compilation, ITypeSymbol? argumentType,
        ITypeSymbol? parameterType)
    {
        if (parameterType?.IsGenericDefinition() == false)
        {
            return compilation.HasImplicitConversion(argumentType, parameterType);
        }
        
        if (parameterType is INamedTypeSymbol { IsGenericType: true, TypeArguments: [{ TypeKind: TypeKind.TypeParameter }] } namedType)
        {
            // `IEnumerable<>`
            if (argumentType is IArrayTypeSymbol { ElementType: { } elementType })
            {
                var specializedSuper = namedType.OriginalDefinition.Construct(elementType);
                return compilation.HasImplicitConversion(argumentType, specializedSuper);
            }

            if (argumentType is INamedTypeSymbol { IsGenericType: true, TypeArguments: [{ } genericArgument] })
            {
                var specializedSuper = namedType.OriginalDefinition.Construct(genericArgument);
                return compilation.HasImplicitConversion(argumentType, specializedSuper);
            }
        }
        else if (parameterType is IArrayTypeSymbol { ElementType.Kind: SymbolKind.TypeParameter })
        {
            // `T[]`
            return true;
        }
        
        return compilation.HasImplicitConversion(argumentType?.OriginalDefinition, parameterType?.OriginalDefinition);
    }
}