using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

internal static class DataSourceAttributeHelper
{
    public static bool IsDataSourceAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return false;
        }

        // Check if the attribute implements IDataSourceAttribute
        return attributeClass.AllInterfaces.Any(i => i.GloballyQualified() == "global::TUnit.Core.IDataSourceAttribute");
    }
    
    public static bool IsTypedDataSourceAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return false;
        }

        // Check if the attribute implements ITypedDataSourceAttribute<T>
        return attributeClass.AllInterfaces.Any(i => 
            i.IsGenericType && 
            i.ConstructedFrom.GloballyQualified() == "global::TUnit.Core.ITypedDataSourceAttribute`1");
    }
    
    public static ITypeSymbol? GetTypedDataSourceType(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
            return null;
            
        var typedInterface = attributeClass.AllInterfaces
            .FirstOrDefault(i => i.IsGenericType && 
                i.ConstructedFrom.GloballyQualified() == "global::TUnit.Core.ITypedDataSourceAttribute`1");
                
        return typedInterface?.TypeArguments.FirstOrDefault();
    }
}