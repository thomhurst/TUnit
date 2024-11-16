using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Extensions;

public static class CompilationExtensions
{
    public static bool HasImplicitConversionOrGenericParameter(this Compilation compilation, ITypeSymbol argumentType,
        ITypeSymbol parameterType)
    {
        if (!parameterType.IsGenericDefinition())
        {
            return compilation.HasImplicitConversion(argumentType, parameterType);
        }
        
        return compilation.HasImplicitConversion(argumentType.OriginalDefinition, parameterType.OriginalDefinition);
    }
}