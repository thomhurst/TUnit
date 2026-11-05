using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Helpers;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

internal static class DataSourceAttributeHelper
{
    // Compare symbol components rather than allocating display strings in row-classification loops.
    // Arguments and method-source attributes also have generic forms with the same identity.
    public static bool IsBuiltIn(INamedTypeSymbol? symbol, string name)
        => symbol is { ContainingType: null } && symbol.Name == name &&
           symbol.ContainingNamespace is
           {
               Name: "Core",
               ContainingNamespace: { Name: "TUnit", ContainingNamespace.IsGlobalNamespace: true }
           };

    public static bool IsArguments(INamedTypeSymbol? symbol) => IsBuiltIn(symbol, "ArgumentsAttribute");

    public static bool IsMethodSource(INamedTypeSymbol? symbol)
        => IsBuiltIn(symbol, "MethodDataSourceAttribute") || IsBuiltIn(symbol, "InstanceMethodDataSourceAttribute");

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
        return InterfaceHelper.ImplementsGenericInterface(attributeClass, "global::TUnit.Core.ITypedDataSourceAttribute<>");
    }

    public static ITypeSymbol? GetTypedDataSourceType(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return null;
        }

        var typedInterface = InterfaceHelper.GetGenericInterface(attributeClass, "global::TUnit.Core.ITypedDataSourceAttribute<>");

        return typedInterface?.TypeArguments.FirstOrDefault();
    }
}
