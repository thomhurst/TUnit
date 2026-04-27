using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Helpers;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

internal static class DataSourceAttributeHelper
{
    // GloballyQualified() emits open generics as Foo<>, not Foo`1
    private const string TypedDataSourceInterfacePattern = "global::TUnit.Core.ITypedDataSourceAttribute<>";

    public static bool IsDataSourceAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return false;
        }

        return InterfaceCache.ImplementsInterface(attributeClass, "global::TUnit.Core.IDataSourceAttribute");
    }

    public static bool IsTypedDataSourceAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return false;
        }

        return InterfaceCache.ImplementsGenericInterface(attributeClass, TypedDataSourceInterfacePattern);
    }

    public static ITypeSymbol? GetTypedDataSourceType(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return null;
        }

        var typedInterface = InterfaceCache.GetGenericInterface(attributeClass, TypedDataSourceInterfacePattern);

        return typedInterface?.TypeArguments.FirstOrDefault();
    }
}