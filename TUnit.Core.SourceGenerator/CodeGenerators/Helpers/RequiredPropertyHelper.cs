using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class RequiredPropertyHelper
{
    /// <summary>
    /// Gets all required properties from a type and its base types
    /// </summary>
    public static IEnumerable<IPropertySymbol> GetAllRequiredProperties(ITypeSymbol typeSymbol)
    {
        var requiredProperties = new List<IPropertySymbol>();
        var currentType = typeSymbol;

        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            var typeRequiredProperties = currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.IsRequired && p.SetMethod?.IsInitOnly == true);

            requiredProperties.AddRange(typeRequiredProperties);
            currentType = currentType.BaseType;
        }

        return requiredProperties;
    }

    /// <summary>
    /// Gets required properties that have data source attributes
    /// </summary>
    public static IEnumerable<IPropertySymbol> GetRequiredPropertiesWithDataSource(ITypeSymbol typeSymbol)
    {
        return GetAllRequiredProperties(typeSymbol)
            .Where(p => HasDataSourceAttribute(p));
    }

    /// <summary>
    /// Gets required properties that don't have data source attributes
    /// </summary>
    public static IEnumerable<IPropertySymbol> GetRequiredPropertiesWithoutDataSource(ITypeSymbol typeSymbol)
    {
        return GetAllRequiredProperties(typeSymbol)
            .Where(p => !HasDataSourceAttribute(p));
    }

    private static bool HasDataSourceAttribute(IPropertySymbol property)
    {
        return property.GetAttributes().Any(attr =>
        {
            var attrName = attr.AttributeClass?.Name ?? "";
            return attrName.EndsWith("DataSourceAttribute") || 
                   attrName == "ClassDataSource" ||
                   attrName == "MethodDataSource" ||
                   attrName == "ArgumentsAttribute" ||
                   attrName == "DataSourceForAttribute";
        });
    }

    /// <summary>
    /// Generates a default value expression for a type
    /// </summary>
    public static string GetDefaultValueForType(ITypeSymbol type)
    {
        if (type.IsReferenceType || type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return "default!";
        }

        return type.SpecialType switch
        {
            SpecialType.System_Boolean => "false",
            SpecialType.System_Char => "'\\0'",
            SpecialType.System_SByte => "0",
            SpecialType.System_Byte => "0",
            SpecialType.System_Int16 => "0",
            SpecialType.System_UInt16 => "0",
            SpecialType.System_Int32 => "0",
            SpecialType.System_UInt32 => "0",
            SpecialType.System_Int64 => "0",
            SpecialType.System_UInt64 => "0",
            SpecialType.System_Decimal => "0m",
            SpecialType.System_Single => "0f",
            SpecialType.System_Double => "0d",
            _ => $"default({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})"
        };
    }
}