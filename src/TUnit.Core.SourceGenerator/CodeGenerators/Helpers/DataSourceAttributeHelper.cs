using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Helpers;
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

        foreach (var implementedInterface in attributeClass.AllInterfaces)
        {
            if (implementedInterface.Name != "IDataSourceAttribute")
            {
                continue;
            }

            // Match the usual interface without allocating its fully qualified display name.
            if (implementedInterface.Arity == 0 && implementedInterface.ContainingType == null &&
                implementedInterface.ContainingNamespace is
                {
                    Name: "Core",
                    ContainingNamespace: { Name: "TUnit", ContainingNamespace.IsGlobalNamespace: true }
                })
            {
                return true;
            }

            // Preserve the existing display-name matching for unusual nested or generic symbols.
            if (implementedInterface.GloballyQualified() == "global::TUnit.Core.IDataSourceAttribute")
            {
                return true;
            }
        }

        return false;
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
