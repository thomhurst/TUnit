using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Helpers;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

internal static class DataSourceAttributeHelper
{
    public static bool IsDataSourceAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return false;
        }

        // Check if the attribute implements IDataSourceAttribute (using cache)
        return InterfaceCache.ImplementsInterface(attributeClass, "global::TUnit.Core.IDataSourceAttribute");
    }

    public static bool IsTypedDataSourceAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return false;
        }

        // Check if the attribute implements ITypedDataSourceAttribute<T> (using cache)
        return InterfaceCache.ImplementsGenericInterface(attributeClass, "global::TUnit.Core.ITypedDataSourceAttribute`1");
    }

    public static ITypeSymbol? GetTypedDataSourceType(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return null;
        }

        var typedInterface = InterfaceCache.GetGenericInterface(attributeClass, "global::TUnit.Core.ITypedDataSourceAttribute`1");

        return typedInterface?.TypeArguments.FirstOrDefault();
    }
}