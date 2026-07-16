using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Helpers;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class AttributeDataExtensions
{
    public static string? GetFullyQualifiedAttributeTypeName(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.GloballyQualifiedNonGeneric();
    }

    public static bool IsTestAttribute(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.GloballyQualified() == WellKnownFullyQualifiedClassNames.TestAttribute.WithGlobalPrefix;
    }

    public static bool IsDataSourceAttribute(this AttributeData? attributeData)
    {
        if (attributeData?.AttributeClass == null)
        {
            return false;
        }

        return InterfaceHelper.ImplementsInterface(attributeData.AttributeClass,
            WellKnownFullyQualifiedClassNames.IDataSourceAttribute.WithGlobalPrefix);
    }

    public static bool IsTypedDataSourceAttribute(this AttributeData? attributeData)
    {
        if (attributeData?.AttributeClass == null)
        {
            return false;
        }

        return InterfaceHelper.ImplementsGenericInterface(attributeData.AttributeClass,
            WellKnownFullyQualifiedClassNames.ITypedDataSourceAttribute.WithGlobalPrefix + "`1");
    }

    public static ITypeSymbol? GetTypedDataSourceType(this AttributeData? attributeData)
    {
        if (attributeData?.AttributeClass == null)
        {
            return null;
        }

        var typedInterface = InterfaceHelper.GetGenericInterface(attributeData.AttributeClass,
            WellKnownFullyQualifiedClassNames.ITypedDataSourceAttribute.WithGlobalPrefix + "`1");

        return typedInterface?.TypeArguments.FirstOrDefault();
    }
}
