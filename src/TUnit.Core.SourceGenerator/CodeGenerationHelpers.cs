using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator;

/// <summary>
/// Helper methods for generating C# code strings from Roslyn symbols and metadata.
/// </summary>
internal static class CodeGenerationHelpers
{
    /// <summary>
    /// Generates direct instantiation code for attributes.
    /// </summary>
    public static string GenerateAttributeInstantiation(AttributeData attr)
    {
        var typeName = attr.AttributeClass!.GloballyQualified();
        using var writer = new CodeWriter("", includeHeader: false);
        writer.SetIndentLevel(1);
        writer.Append($"new {typeName}(");

        if (attr.ConstructorArguments.Length > 0)
        {
            var argStrings = new List<string>();

            for (var i = 0; i < attr.ConstructorArguments.Length; i++)
            {
                var arg = attr.ConstructorArguments[i];

                // Expand a params array into individual arguments
                if (i == attr.ConstructorArguments.Length - 1
                    && arg.Kind == TypedConstantKind.Array
                    && IsParamsArrayArgument(attr))
                {
                    if (!arg.Values.IsDefault)
                    {
                        argStrings.AddRange(arg.Values.Select(v => TypedConstantParser.GetRawTypedConstantValue(v)));
                    }
                }
                else
                {
                    argStrings.Add(TypedConstantParser.GetRawTypedConstantValue(arg));
                }
            }

            writer.AppendJoin(", ", argStrings);
        }

        writer.Append(")");

        if (attr.NamedArguments.Length > 0)
        {
            writer.Append(" { ");
            var namedArgs = attr.NamedArguments.Select(na => $"{na.Key} = {TypedConstantParser.GetRawTypedConstantValue(na.Value)}");
            writer.AppendJoin(", ", namedArgs);
            writer.Append(" }");
        }

        return writer.ToString().Trim();
    }

    /// <summary>
    /// Determines if an argument is for a params array parameter.
    /// </summary>
    private static bool IsParamsArrayArgument(AttributeData attr)
    {
        var typeName = attr.AttributeClass!.GloballyQualified();

        return typeName is "global::TUnit.Core.ArgumentsAttribute" or "global::TUnit.Core.InlineDataAttribute";
    }

    /// <summary>
    /// Determines if an attribute should be excluded from metadata.
    /// </summary>
    private static bool IsCompilerGeneratedAttribute(AttributeData attr)
    {
        var fullName = attr.AttributeClass!.GloballyQualified();
        return fullName.StartsWith("System.Runtime.CompilerServices.") ||
               fullName.StartsWith("System.Diagnostics.CodeAnalysis.");
    }

    public static bool ContainsTypeParameter(ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol)
        {
            return true;
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return ContainsTypeParameter(arrayType.ElementType);
        }

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            return namedType.TypeArguments.Any(ContainsTypeParameter);
        }

        return false;
    }

    /// <summary>
    /// Determines if an attribute is a data source attribute.
    /// </summary>
    private static bool IsDataSourceAttribute(AttributeData attr)
    {
        if (attr.AttributeClass == null)
        {
            return false;
        }

        // Check if the attribute implements IDataSourceAttribute
        return attr.AttributeClass.AllInterfaces.Any(i => i.GloballyQualified() == "global::TUnit.Core.IDataSourceAttribute");
    }

    /// <summary>
    /// Generates all test-related attributes for the TestMetadata.AttributesByType field as a dictionary.
    /// </summary>
    public static string GenerateTestAttributes(IMethodSymbol methodSymbol)
    {
        // Include all attributes from both method and class that might be relevant for test filtering/metadata
        var allAttributes = methodSymbol.GetAttributes()
            .Concat(methodSymbol.ContainingType.GetAttributes())
            .Where(attr => !IsCompilerGeneratedAttribute(attr) && !IsDataSourceAttribute(attr))
            .ToList();

        if (allAttributes.Count == 0)
        {
            return "new global::System.Collections.Generic.Dictionary<global::System.Type, global::System.Collections.Generic.IReadOnlyList<global::System.Attribute>>().AsReadOnly()";
        }

        // Group attributes by type
        var attributesByType = allAttributes
            .GroupBy(attr => attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "System.Attribute");

        using var writer = new CodeWriter("", includeHeader: false);

        // Generate dictionary initializer
        writer.Append("new global::System.Collections.Generic.Dictionary<global::System.Type, global::System.Collections.Generic.IReadOnlyList<global::System.Attribute>>()");
        writer.AppendLine();
        writer.AppendLine("{");
        writer.Indent();

        foreach (var group in attributesByType)
        {
            var typeString = group.Key;

            writer.Append($"[typeof({typeString})] = new global::System.Attribute[] {{ ");

            var attributeStrings = new List<string>();
            foreach (var attr in group)
            {
                attributeStrings.Add(GenerateAttributeInstantiation(attr));
            }

            writer.AppendJoin(", ", attributeStrings);
            writer.AppendLine(" },");
        }

        writer.Unindent();
        writer.Append("}.AsReadOnly()");

        return writer.ToString().Trim();
    }

    /// <summary>
    /// Generates C# code to create a TypeInfo from an ITypeSymbol.
    /// </summary>
    public static string GenerateTypeInfo(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is ITypeParameterSymbol typeParameter)
        {
            // This is a generic parameter (e.g., T, TKey)
            var position = GetGenericParameterPosition(typeParameter);
            var isMethodParameter = typeParameter.DeclaringMethod != null;
            return $@"new global::TUnit.Core.GenericParameter({position}, {(isMethodParameter ? "true" : "false")}, ""{typeParameter.Name}"")";
        }

        if (typeSymbol is INamedTypeSymbol { IsGenericType: true, IsUnboundGenericType: false } namedType)
        {
            // This is a constructed generic type (e.g., List<int>, Dictionary<string, T>)
            // Check if all type arguments are concrete (no generic parameters)
            var hasGenericParameters = namedType.TypeArguments.Any(ContainsTypeParameter);

            if (hasGenericParameters)
            {
                // Has generic parameters - use ConstructedGeneric
                var genericDefType = GetTypeOfExpression(namedType.ConstructUnboundGenericType());
                var genericArgs = namedType.TypeArguments.Select(GenerateTypeInfo).ToArray();

                using var writer = new CodeWriter("", includeHeader: false);
                writer.SetIndentLevel(1);
                writer.Append($@"new global::TUnit.Core.ConstructedGeneric({genericDefType}, [{string.Join(", ", genericArgs)}])");
                return writer.ToString().Trim();
            }
            // All type arguments are concrete - this is just a concrete type
            // Fall through to regular typeof() handling
        }

        // Regular concrete type (including fully closed generic types like List<int>)
        var typeOfExpression = GetTypeOfExpression(typeSymbol);
        return $@"new global::TUnit.Core.ConcreteType({typeOfExpression})";
    }

    /// <summary>
    /// Generates a typeof() expression for the given type symbol.
    /// </summary>
    private static string GetTypeOfExpression(ITypeSymbol typeSymbol)
    {
        var fullyQualifiedName = typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        return $"typeof({fullyQualifiedName})";
    }

    private static int GetGenericParameterPosition(ITypeParameterSymbol typeParameter)
    {
        if (typeParameter.DeclaringMethod != null)
        {
            return typeParameter.DeclaringMethod.TypeParameters.IndexOf(typeParameter);
        }
        if (typeParameter.DeclaringType != null)
        {
            return typeParameter.DeclaringType.TypeParameters.IndexOf(typeParameter);
        }
        return 0;
    }
}
