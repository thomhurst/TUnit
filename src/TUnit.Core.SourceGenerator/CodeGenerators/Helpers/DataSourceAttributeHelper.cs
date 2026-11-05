using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

    public static bool IsOrInheritsBuiltIn(INamedTypeSymbol? symbol, string name)
    {
        for (; symbol is not null; symbol = symbol.BaseType)
        {
            if (IsBuiltIn(symbol, name))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsMethodSource(INamedTypeSymbol? symbol)
        => IsOrInheritsBuiltIn(symbol, "MethodDataSourceAttribute") || IsOrInheritsBuiltIn(symbol, "InstanceMethodDataSourceAttribute");

    // Only interpret a custom constructor's arguments when it forwards them unchanged to a
    // built-in constructor. Unknown mappings stay on the runtime IDataSourceAttribute path.
    public static bool CanReadMethodSourceArguments(AttributeData attribute)
    {
        if (IsBuiltIn(attribute.AttributeClass, "MethodDataSourceAttribute")
            || IsBuiltIn(attribute.AttributeClass, "InstanceMethodDataSourceAttribute"))
        {
            return true;
        }

        if (attribute.AttributeClass is not { IsGenericType: false, BaseType.IsGenericType: false }
            || attribute.AttributeConstructor is not { } constructor
            || !(IsBuiltIn(attribute.AttributeClass?.BaseType, "MethodDataSourceAttribute")
                || IsBuiltIn(attribute.AttributeClass?.BaseType, "InstanceMethodDataSourceAttribute")))
        {
            return false;
        }

        foreach (var reference in constructor.DeclaringSyntaxReferences)
        {
            var arguments = reference.GetSyntax() switch
            {
                ClassDeclarationSyntax declaration => declaration.BaseList?.Types
                    .OfType<PrimaryConstructorBaseTypeSyntax>().SingleOrDefault()?.ArgumentList.Arguments,
                ConstructorDeclarationSyntax { Body.Statements.Count: 0, ExpressionBody: null } declaration
                    when declaration.Initializer?.ThisOrBaseKeyword.ValueText == "base"
                    => declaration.Initializer.ArgumentList.Arguments,
                _ => null
            };

            if (arguments is not { } forwarded || forwarded.Count != constructor.Parameters.Length)
            {
                continue;
            }

            for (var i = 0; i < forwarded.Count; i++)
            {
                if (forwarded[i].NameColon != null
                    || forwarded[i].Expression is not IdentifierNameSyntax identifier
                    || identifier.Identifier.ValueText != constructor.Parameters[i].Name)
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

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
