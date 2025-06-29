using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Analyzers.Extensions;

public static class CompilationExtensions
{
    public static bool HasImplicitConversionOrGenericParameter(this Compilation compilation, ITypeSymbol? argumentType,
        ITypeSymbol? parameterType)
    {
        if (parameterType?.IsGenericDefinition() == false)
        {
            if (argumentType is null)
            {
                return false;
            }

            var conversion = compilation.ClassifyConversion(argumentType, parameterType);
            return conversion.IsImplicit || conversion.IsNumeric;
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

        return compilation.HasImplicitConversion(argumentType?.OriginalDefinition, parameterType?.OriginalDefinition);
    }
}
