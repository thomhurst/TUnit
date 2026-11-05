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
    // built-in constructor. Source syntax is intentionally required: metadata-only constructors,
    // constructor chains, and other unproven shapes stay on the runtime IDataSourceAttribute path.
    public static bool CanReadMethodSourceArguments(AttributeData attribute)
    {
        if (IsBuiltIn(attribute.AttributeClass, "MethodDataSourceAttribute")
            || IsBuiltIn(attribute.AttributeClass, "InstanceMethodDataSourceAttribute"))
        {
            return true;
        }

        if (attribute.AttributeClass is not { IsGenericType: false, BaseType.IsGenericType: false } attributeClass
            || attribute.AttributeConstructor is not { } constructor
            || !(IsBuiltIn(attribute.AttributeClass?.BaseType, "MethodDataSourceAttribute")
                || IsBuiltIn(attribute.AttributeClass?.BaseType, "InstanceMethodDataSourceAttribute")))
        {
            return false;
        }

        // Named custom or hidden setters can mutate the source after construction. Only trust
        // supplied members that resolve to the built-in configuration properties.
        foreach (var namedArgument in attribute.NamedArguments)
        {
            ISymbol? member = null;
            for (var type = attributeClass; type is not null; type = type.BaseType)
            {
                member = type.GetMembers(namedArgument.Key).FirstOrDefault();
                if (member is not null)
                {
                    break;
                }
            }

            if (member is not IPropertySymbol property
                || !IsBuiltIn(property.ContainingType, "MethodDataSourceAttribute"))
            {
                return false;
            }
        }

        // Forwarding the constructor is insufficient if the subclass replaces the row provider.
        // Resolve the interface slot so both explicit and implicit reimplementations use runtime rows.
        var sourceInterface = attributeClass.AllInterfaces
            .FirstOrDefault(type => IsBuiltIn(type, "IDataSourceAttribute"));
        var rowMethod = sourceInterface?.GetMembers("GetDataRowsAsync").OfType<IMethodSymbol>().SingleOrDefault();
        if (rowMethod is null
            || attributeClass.FindImplementationForInterfaceMember(rowMethod) is not IMethodSymbol implementation
            || !IsBuiltIn(implementation.ContainingType, "MethodDataSourceAttribute"))
        {
            return false;
        }

        foreach (var reference in constructor.DeclaringSyntaxReferences)
        {
            var arguments = reference.GetSyntax() switch
            {
                ClassDeclarationSyntax declaration => declaration.BaseList?.Types
                    .OfType<PrimaryConstructorBaseTypeSyntax>().SingleOrDefault()?.ArgumentList.Arguments,
                ConstructorDeclarationSyntax { Body: { } body, ExpressionBody: null } declaration
                    when declaration.Initializer?.ThisOrBaseKeyword.ValueText == "base"
                        && body.Statements.All(statement => IsIndependentConfiguration(statement, constructor))
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

    // Only literal writes to the built-in boolean options are independent of row selection.
    // Reject calls, source/row writes, and hidden members whose setters could change the source.
    private static bool IsIndependentConfiguration(StatementSyntax statement, IMethodSymbol constructor)
    {
        if (statement is not ExpressionStatementSyntax
            {
                Expression: AssignmentExpressionSyntax
                {
                    RawKind: (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleAssignmentExpression,
                    Right: LiteralExpressionSyntax { Token.Value: bool }
                } assignment
            })
        {
            return false;
        }

        var name = assignment.Left switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax or BaseExpressionSyntax, Name: IdentifierNameSyntax identifier }
                => identifier.Identifier.ValueText,
            _ => null
        };

        if (name is not ("SkipIfEmpty" or "DeferEnumeration"))
        {
            return false;
        }

        return assignment.Left is MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax }
            || (constructor.ContainingType.GetMembers(name).Length == 0
                && (assignment.Left is not IdentifierNameSyntax
                    || !constructor.Parameters.Any(parameter => parameter.Name == name)));
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
