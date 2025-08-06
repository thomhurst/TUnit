using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Utilities;

/// <summary>
/// Centralized helper for generating metadata object instantiation code
/// </summary>
internal static class MetadataGenerationHelper
{
    /// <summary>
    /// Generates code for creating a MethodMetadata instance
    /// </summary>
    public static string GenerateMethodMetadata(IMethodSymbol methodSymbol, string classMetadataExpression, int currentIndentLevel = 0)
    {
        var writer = new CodeWriter("", includeHeader: false).SetIndentLevel(currentIndentLevel);
        writer.AppendLine("new global::TUnit.Core.MethodMetadata");
        writer.AppendLine("{");
        writer.Indent();

        var safeTypeDisplay = methodSymbol.ContainingType.GloballyQualified();
        var safeReturnTypeDisplay = methodSymbol.ReturnType.GloballyQualified();

        writer.AppendLine($"Type = typeof({safeTypeDisplay}),");
        writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(methodSymbol.ContainingType)},");
        writer.AppendLine($"Name = \"{methodSymbol.Name}\",");
        writer.AppendLine($"GenericTypeCount = {methodSymbol.TypeParameters.Length},");
        writer.AppendLine($"ReturnType = typeof({safeReturnTypeDisplay}),");
        writer.AppendLine($"ReturnTypeReference = {CodeGenerationHelpers.GenerateTypeReference(methodSymbol.ReturnType)},");
        writer.AppendLine($"Parameters = {GenerateParameterMetadataArrayForMethod(methodSymbol, writer.IndentLevel)},");
        writer.AppendLine($"Class = {classMetadataExpression}");

        writer.Unindent();
        writer.Append("}");

        return writer.ToString();
    }

    /// <summary>
    /// Generates code for creating a ClassMetadata instance with GetOrAdd pattern
    /// </summary>
    public static string GenerateClassMetadataGetOrAdd(INamedTypeSymbol typeSymbol, string? parentExpression = null, int currentIndentLevel = 0)
    {
        var qualifiedName = $"{typeSymbol.ContainingAssembly.Name}:{typeSymbol.GloballyQualified()}";
        var writer = new CodeWriter("", includeHeader: false).SetIndentLevel(currentIndentLevel);
        writer.AppendLine($"global::TUnit.Core.ClassMetadata.GetOrAdd(\"{qualifiedName}\", () => ");
        writer.AppendLine("{");
        writer.Indent();

        // Create the ClassMetadata instance
        writer.AppendLine("var classMetadata = new global::TUnit.Core.ClassMetadata");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"Type = typeof({typeSymbol.GloballyQualified()}),");
        writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(typeSymbol)},");
        writer.AppendLine($"Name = \"{typeSymbol.Name}\",");
        writer.AppendLine($"Namespace = \"{typeSymbol.ContainingNamespace?.ToDisplayString() ?? ""}\",");
        writer.AppendLine($"Assembly = {GenerateAssemblyMetadataGetOrAdd(typeSymbol.ContainingAssembly)},");
        var constructor = typeSymbol.InstanceConstructors.FirstOrDefault();
        var constructorParams = constructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        if (constructor != null && constructorParams.Length > 0)
        {
            writer.AppendLine($"Parameters = {GenerateParameterMetadataArrayForConstructor(constructor, typeSymbol, writer.IndentLevel)},");
        }
        else
        {
            writer.AppendLine("Parameters = global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>(),");
        }
        writer.AppendLine($"Properties = {GeneratePropertyMetadataArray(typeSymbol, writer.IndentLevel)},");
        writer.AppendLine($"Parent = {parentExpression ?? "null"}");

        writer.Unindent();
        writer.AppendLine("};");
        writer.AppendLine();

        // Set ClassMetadata reference on each property
        writer.AppendLine("// Set ClassMetadata and ContainingTypeMetadata references on properties to avoid circular dependency");
        writer.AppendLine("foreach (var prop in classMetadata.Properties)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("prop.ClassMetadata = classMetadata;");
        writer.AppendLine("prop.ContainingTypeMetadata = classMetadata;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();

        writer.AppendLine("return classMetadata;");

        writer.Unindent();
        writer.Append("})");

        return writer.ToString();
    }

    /// <summary>
    /// Generates code for creating an AssemblyMetadata instance with GetOrAdd pattern
    /// </summary>
    public static string GenerateAssemblyMetadataGetOrAdd(IAssemblySymbol assembly)
    {
        return $"global::TUnit.Core.AssemblyMetadata.GetOrAdd(\"{assembly.Name}\", () => new global::TUnit.Core.AssemblyMetadata {{ Name = \"{assembly.Name}\" }})";
    }

    /// <summary>
    /// Generates code for creating a ParameterMetadata instance (generic version)
    /// </summary>
    public static string GenerateParameterMetadataGeneric(IParameterSymbol parameter, IMethodSymbol? containingMethod = null)
    {
        // For type parameters in generic context, we still can't use T directly
        var safeType = CodeGenerationHelpers.ContainsTypeParameter(parameter.Type) ? "object" : parameter.Type.GloballyQualified();
        var reflectionInfo = GenerateReflectionInfoForParameter(parameter, containingMethod);

        return $@"new global::TUnit.Core.ParameterMetadata<{safeType}>
{{
    Name = ""{parameter.Name}"",
    TypeReference = {CodeGenerationHelpers.GenerateTypeReference(parameter.Type)},
    ReflectionInfo = {reflectionInfo}
}}";
    }

    /// <summary>
    /// Generates code for creating a ParameterMetadata instance (non-generic version)
    /// </summary>
    public static string GenerateParameterMetadata(IParameterSymbol parameter, IMethodSymbol? containingMethod = null)
    {
        // For type parameters, we need to use typeof(object) instead of typeof(T)
        var typeForConstructor = CodeGenerationHelpers.ContainsTypeParameter(parameter.Type) ? "object" : parameter.Type.GloballyQualified();
        var reflectionInfo = GenerateReflectionInfoForParameter(parameter, containingMethod);

        return $@"new global::TUnit.Core.ParameterMetadata(typeof({typeForConstructor}))
{{
    Name = ""{parameter.Name}"",
    TypeReference = {CodeGenerationHelpers.GenerateTypeReference(parameter.Type)},
    IsNullable = {parameter.Type.IsNullable().ToString().ToLowerInvariant()},
    ReflectionInfo = {reflectionInfo}
}}";
    }

    /// <summary>
    /// Generates reflection info code for a parameter
    /// </summary>
    private static string GenerateReflectionInfoForParameter(IParameterSymbol parameter, IMethodSymbol? providedMethod = null)
    {
        // Use provided method or try to get it from the parameter's containing symbol
        var method = providedMethod ?? parameter.ContainingSymbol as IMethodSymbol;

        if (method == null)
        {
            // If we can't determine the method, fall back to null
            return "null!";
        }

        // Find the parameter index
        var parameterIndex = method.Parameters.IndexOf(parameter);
        if (parameterIndex == -1)
        {
            // Fallback if we can't find the parameter (shouldn't happen)
            return "null!";
        }

        var containingType = method.ContainingType.GloballyQualified();

        // Check if it's a constructor
        if (method.MethodKind == MethodKind.Constructor)
        {
            if (method.Parameters.Any(p => CodeGenerationHelpers.ContainsTypeParameter(p.Type)))
            {
                // For constructors with generic parameters, we need to find it dynamically
                return $@"typeof({containingType}).GetConstructors().FirstOrDefault(c => c.GetParameters().Length == {method.Parameters.Length})?.GetParameters()[{parameterIndex}]!";
            }
            else
            {
                // For non-generic constructors, we can use specific parameter types
                var paramTypes = GenerateParameterTypesArrayForReflection(method);
                return $@"typeof({containingType}).GetConstructor({paramTypes})!.GetParameters()[{parameterIndex}]";
            }
        }
        else
        {
            // It's a regular method
            if (method.TypeParameters.Length > 0 || method.Parameters.Any(p => CodeGenerationHelpers.ContainsTypeParameter(p.Type)))
            {
                // For generic methods, use GetMethods and find by name
                return $@"typeof({containingType}).GetMethods(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Static).FirstOrDefault(m => m.Name == ""{method.Name}"" && m.GetParameters().Length == {method.Parameters.Length})?.GetParameters()[{parameterIndex}]!";
            }
            else
            {
                // For non-generic methods, we can use GetMethod with parameter types
                var bindingFlags = method.IsStatic ? "global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static" : "global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance";
                var paramTypes = GenerateParameterTypesArrayForReflection(method);
                return $@"typeof({containingType}).GetMethod(""{method.Name}"", {bindingFlags}, null, {paramTypes}, null)!.GetParameters()[{parameterIndex}]";
            }
        }
    }


    private static string GenerateParameterTypesArrayForReflection(IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
        {
            return "global::System.Type.EmptyTypes";
        }

        var paramTypes = method.Parameters.Select(p =>
        {
            var safeTypeName = p.Type.GloballyQualified();
            return $"typeof({safeTypeName})";
        });

        return $"new global::System.Type[] {{ {string.Join(", ", paramTypes)} }}";
    }

    /// <summary>
    /// Generates code for creating a PropertyMetadata instance
    /// </summary>
    public static string GeneratePropertyMetadata(IPropertySymbol property, INamedTypeSymbol containingType)
    {
        var safeTypeNameForReflection = containingType.GloballyQualified();
        // For type parameters, we need to use typeof(object) instead of typeof(T)
        var safePropertyTypeName = CodeGenerationHelpers.ContainsTypeParameter(property.Type) ? "object" : property.Type.GloballyQualified();

        return $@"new global::TUnit.Core.PropertyMetadata
{{
    ReflectionInfo = typeof({safeTypeNameForReflection}).GetProperty(""{property.Name}""),
    Type = typeof({safePropertyTypeName}),
    Name = ""{property.Name}"",
    IsStatic = {property.IsStatic.ToString().ToLower()},
    IsNullable = {property.Type.IsNullable().ToString().ToLowerInvariant()},
    Getter = {GetPropertyAccessor(containingType, property)},
    ClassMetadata = null!,
    ContainingTypeMetadata = null!
}}";
    }

    /// <summary>
    /// Generates property accessor lambda expression
    /// </summary>
    private static string GetPropertyAccessor(INamedTypeSymbol namedTypeSymbol, IPropertySymbol property)
    {
        // For generic types with unresolved type parameters, we can't cast to the open generic type
        // We need to use dynamic or reflection
        var hasUnresolvedTypeParameters = namedTypeSymbol.IsGenericType &&
                                         namedTypeSymbol.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);

        if (hasUnresolvedTypeParameters && !property.IsStatic)
        {
            // Use dynamic to avoid invalid cast to open generic type
            return $"o => ((dynamic)o).{property.Name}";
        }

        var safeTypeName = namedTypeSymbol.GloballyQualified();
        return property.IsStatic
            ? $"_ => {safeTypeName}.{property.Name}"
            : $"o => (({safeTypeName})o).{property.Name}";
    }

    /// <summary>
    /// Generates an array of ParameterMetadata objects for method parameters with proper reflection info
    /// </summary>
    private static string GenerateParameterMetadataArrayForMethod(IMethodSymbol method, int currentIndentLevel = 0)
    {
        if (method.Parameters.Length == 0)
        {
            return "global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>()";
        }

        var writer = new CodeWriter("", includeHeader: false).SetIndentLevel(currentIndentLevel);
        writer.AppendLine("new global::TUnit.Core.ParameterMetadata[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var param in method.Parameters)
        {
            writer.AppendLine($"{GenerateParameterMetadata(param, method)},");
        }

        writer.Unindent();
        writer.Append("}");

        return writer.ToString();
    }

    /// <summary>
    /// Generates an array of ParameterMetadata objects for constructor parameters with proper reflection info
    /// </summary>
    private static string GenerateParameterMetadataArrayForConstructor(IMethodSymbol constructor, INamedTypeSymbol containingType, int currentIndentLevel = 0)
    {
        if (constructor.Parameters.Length == 0)
        {
            return "global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>()";
        }

        var writer = new CodeWriter("", includeHeader: false).SetIndentLevel(currentIndentLevel);
        writer.AppendLine("new global::TUnit.Core.ParameterMetadata[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var param in constructor.Parameters)
        {
            writer.AppendLine($"{GenerateParameterMetadata(param, constructor)},");
        }

        writer.Unindent();
        writer.Append("}");

        return writer.ToString();
    }

    /// <summary>
    /// Generates an array of PropertyMetadata objects
    /// </summary>
    private static string GeneratePropertyMetadataArray(INamedTypeSymbol typeSymbol, int currentIndentLevel = 0)
    {
        var properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.Name != "EqualityContract")
            .ToList();

        if (!properties.Any())
        {
            return "global::System.Array.Empty<global::TUnit.Core.PropertyMetadata>()";
        }

        var writer = new CodeWriter("", includeHeader: false).SetIndentLevel(currentIndentLevel);
        writer.AppendLine("new global::TUnit.Core.PropertyMetadata[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var prop in properties)
        {
            writer.AppendLine($"{GeneratePropertyMetadata(prop, typeSymbol)},");
        }

        writer.Unindent();
        writer.Append("}");

        return writer.ToString();
    }
}
