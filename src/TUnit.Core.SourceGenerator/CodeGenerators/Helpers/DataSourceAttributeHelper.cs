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

        // Check if the attribute implements IDataSourceAttribute
        return InterfaceHelper.ImplementsInterface(attributeClass, "global::TUnit.Core.IDataSourceAttribute");
    }

    public static bool IsTypedDataSourceAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return false;
        }

        // Check if the attribute implements ITypedDataSourceAttribute<T>
        return InterfaceHelper.ImplementsGenericInterface(attributeClass, "global::TUnit.Core.ITypedDataSourceAttribute`1");
    }

    public static ITypeSymbol? GetTypedDataSourceType(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return null;
        }

        var typedInterface = InterfaceHelper.GetGenericInterface(attributeClass, "global::TUnit.Core.ITypedDataSourceAttribute`1");

        return typedInterface?.TypeArguments.FirstOrDefault();
    }
}