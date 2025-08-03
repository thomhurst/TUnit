using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

internal static class TypeSymbolExtensions
{
    /// <summary>
    /// Determines if the type is a nullable value type (e.g., int?, bool?, MyEnum?)
    /// </summary>
    public static bool IsNullableValueType(this ITypeSymbol typeSymbol)
    {
        return typeSymbol is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T };
    }
    
    /// <summary>
    /// Determines if the type is nullable (either nullable value type or nullable reference type)
    /// </summary>
    public static bool IsNullable(this ITypeSymbol typeSymbol)
    {
        // Check for nullable value types (e.g., int?, bool?)
        if (IsNullableValueType(typeSymbol))
        {
            return true;
        }
        
        // Check for nullable reference types (e.g., string?)
        return typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
    }
    
    /// <summary>
    /// Gets the underlying type of a nullable value type, or null if the type is not nullable
    /// </summary>
    public static ITypeSymbol? GetNullableUnderlyingType(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } namedType)
        {
            return namedType.TypeArguments[0];
        }
        return null;
    }
}