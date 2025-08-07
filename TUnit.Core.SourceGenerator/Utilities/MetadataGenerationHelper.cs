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
    /// Writes a multi-line string with proper indentation
    /// </summary>
    private static void WriteIndentedString(ICodeWriter writer, string multiLineString, bool firstLineIsInline = true)
    {
        var lines = multiLineString.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        // Find the base indentation level from the content (skip first line as it's usually inline)
        int baseIndent = 0;
        if (lines.Length > 1)
        {
            // Find first non-empty line after the first to determine base indentation
            for (int i = 1; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    baseIndent = lines[i].Length - lines[i].TrimStart().Length;
                    break;
                }
            }
        }
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                writer.AppendLine();
            }
            
            var line = lines[i];
            if (!string.IsNullOrWhiteSpace(line))
            {
                // Calculate how much indentation this line has beyond the base
                var currentIndent = line.Length - line.TrimStart().Length;
                var relativeIndent = Math.Max(0, currentIndent - baseIndent);
                
                // Add relative indentation
                for (int j = 0; j < relativeIndent; j++)
                {
                    writer.Append(" ");
                }
                
                writer.Append(line.TrimStart());
            }
        }
    }
    
    /// <summary>
    /// Writes code for creating a MethodMetadata instance
    /// </summary>
    public static void WriteMethodMetadata(ICodeWriter writer, IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol)
    {
        writer.AppendLine("new global::TUnit.Core.MethodMetadata");
        writer.AppendLine("{");
        
        // Manually increment indent level without calling EnsureNewLine
        var currentIndent = writer.IndentLevel;
        writer.SetIndentLevel(currentIndent + 1);

        var safeTypeDisplay = methodSymbol.ContainingType.GloballyQualified();
        var safeReturnTypeDisplay = methodSymbol.ReturnType.GloballyQualified();

        writer.AppendLine($"Type = typeof({safeTypeDisplay}),");
        writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(methodSymbol.ContainingType)},");
        writer.AppendLine($"Name = \"{methodSymbol.Name}\",");
        writer.AppendLine($"GenericTypeCount = {methodSymbol.TypeParameters.Length},");
        writer.AppendLine($"ReturnType = typeof({safeReturnTypeDisplay}),");
        writer.AppendLine($"ReturnTypeReference = {CodeGenerationHelpers.GenerateTypeReference(methodSymbol.ReturnType)},");
        writer.Append($"Parameters = ");
        WriteParameterMetadataArrayForMethod(writer, methodSymbol);
        writer.AppendLine(",");
        writer.Append("Class = ");
        WriteClassMetadataGetOrAdd(writer, namedTypeSymbol);

        // Manually restore indent level
        writer.SetIndentLevel(currentIndent);
        writer.AppendLine();
        writer.Append("}");
    }
    
    /// <summary>
    /// Generates code for creating a MethodMetadata instance (for backward compat)
    /// </summary>
    public static string GenerateMethodMetadata(IMethodSymbol methodSymbol, string classMetadataExpression, int currentIndentLevel = 0)
    {
        // Can't use the new WriteMethodMetadata because it takes INamedTypeSymbol, not a string expression
        // So we keep the old implementation for backward compat
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
    /// Writes code for creating a ClassMetadata instance with GetOrAdd pattern
    /// </summary>
    private static void WriteClassMetadataGetOrAdd(ICodeWriter writer, INamedTypeSymbol typeSymbol, string? parentExpression = null)
    {
        var qualifiedName = $"{typeSymbol.ContainingAssembly.Name}:{typeSymbol.GloballyQualified()}";
        writer.AppendLine($"global::TUnit.Core.ClassMetadata.GetOrAdd(\"{qualifiedName}\", () => ");
        writer.AppendLine("{");
        
        // Manually increment indent level without calling EnsureNewLine
        var currentIndent = writer.IndentLevel;
        writer.SetIndentLevel(currentIndent + 1);

        // Create the ClassMetadata instance
        writer.AppendLine("var classMetadata = new global::TUnit.Core.ClassMetadata");
        writer.AppendLine("{");
        
        // Increment for the object initializer content
        writer.SetIndentLevel(currentIndent + 2);

        writer.AppendLine($"Type = typeof({typeSymbol.GloballyQualified()}),");
        writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(typeSymbol)},");
        writer.AppendLine($"Name = \"{typeSymbol.Name}\",");
        writer.AppendLine($"Namespace = \"{typeSymbol.ContainingNamespace?.ToDisplayString() ?? ""}\",");
        writer.AppendLine($"Assembly = {GenerateAssemblyMetadataGetOrAdd(typeSymbol.ContainingAssembly)},");
        
        var constructor = typeSymbol.InstanceConstructors.FirstOrDefault();
        var constructorParams = constructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        if (constructor != null && constructorParams.Length > 0)
        {
            writer.Append("Parameters = ");
            WriteParameterMetadataArrayForConstructor(writer, constructor, typeSymbol);
            writer.AppendLine(",");
        }
        else
        {
            writer.AppendLine("Parameters = global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>(),");
        }
        
        writer.Append("Properties = ");
        WritePropertyMetadataArray(writer, typeSymbol);
        writer.AppendLine(",");
        writer.Append($"Parent = {parentExpression ?? "null"}");

        // Back to lambda body level
        writer.SetIndentLevel(currentIndent + 1);
        writer.AppendLine();
        writer.AppendLine("};");
        
        // Set ClassMetadata reference on each property
        writer.AppendLine("// Set ClassMetadata and ContainingTypeMetadata references on properties to avoid circular dependency");
        writer.AppendLine("foreach (var prop in classMetadata.Properties)");
        writer.AppendLine("{");
        
        writer.SetIndentLevel(currentIndent + 2);
        writer.AppendLine("prop.ClassMetadata = classMetadata;");
        writer.Append("prop.ContainingTypeMetadata = classMetadata;");
        
        writer.SetIndentLevel(currentIndent + 1);
        writer.AppendLine();
        writer.AppendLine("}");
        writer.Append("return classMetadata;");
        
        // Back to original level
        writer.SetIndentLevel(currentIndent);
        writer.AppendLine();
        writer.Append("})");
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
    public static void WriteParameterMetadataGeneric(ICodeWriter writer, IParameterSymbol parameter, IMethodSymbol? containingMethod = null)
    {
        // For type parameters in generic context, we still can't use T directly
        var safeType = CodeGenerationHelpers.ContainsTypeParameter(parameter.Type) ? "object" : parameter.Type.GloballyQualified();
        var reflectionInfo = GenerateReflectionInfoForParameter(parameter, containingMethod);

        writer.AppendLine($"new global::TUnit.Core.ParameterMetadata<{safeType}>");
        writer.AppendLine("{");
        
        // Manually increment indent level without calling EnsureNewLine
        var currentIndent = writer.IndentLevel;
        writer.SetIndentLevel(currentIndent + 1);
        
        writer.AppendLine($"Name = \"{parameter.Name}\",");
        writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(parameter.Type)},");
        writer.Append($"ReflectionInfo = {reflectionInfo}");
        
        // Manually restore indent level
        writer.SetIndentLevel(currentIndent);
        writer.AppendLine();
        writer.Append("}");
    }

    /// <summary>
    /// Generates code for creating a ParameterMetadata instance (non-generic version)
    /// </summary>
    public static void WriteParameterMetadata(ICodeWriter writer, IParameterSymbol parameter, IMethodSymbol? containingMethod = null)
    {
        // For type parameters, we need to use typeof(object) instead of typeof(T)
        var typeForConstructor = CodeGenerationHelpers.ContainsTypeParameter(parameter.Type) ? "object" : parameter.Type.GloballyQualified();
        var reflectionInfo = GenerateReflectionInfoForParameter(parameter, containingMethod);

        writer.AppendLine($"new global::TUnit.Core.ParameterMetadata(typeof({typeForConstructor}))");
        writer.AppendLine("{");
        
        // Manually increment indent level without calling EnsureNewLine
        var currentIndent = writer.IndentLevel;
        writer.SetIndentLevel(currentIndent + 1);
        
        writer.AppendLine($"Name = \"{parameter.Name}\",");
        writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(parameter.Type)},");
        writer.AppendLine($"IsNullable = {parameter.Type.IsNullable().ToString().ToLowerInvariant()},");
        writer.Append($"ReflectionInfo = {reflectionInfo}");
        
        // Manually restore indent level
        writer.SetIndentLevel(currentIndent);
        writer.AppendLine();
        writer.Append("}");
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
                return $@"global::System.Linq.Enumerable.FirstOrDefault(typeof({containingType}).GetConstructors(), c => c.GetParameters().Length == {method.Parameters.Length})?.GetParameters()[{parameterIndex}]!";
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
                return $@"global::System.Linq.Enumerable.FirstOrDefault(typeof({containingType}).GetMethods(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Static), m => m.Name == ""{method.Name}"" && m.GetParameters().Length == {method.Parameters.Length})?.GetParameters()[{parameterIndex}]!";
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
    public static void WritePropertyMetadata(ICodeWriter writer, IPropertySymbol property, INamedTypeSymbol containingType)
    {
        var safeTypeNameForReflection = containingType.GloballyQualified();
        // For type parameters, we need to use typeof(object) instead of typeof(T)
        var safePropertyTypeName = CodeGenerationHelpers.ContainsTypeParameter(property.Type) ? "object" : property.Type.GloballyQualified();

        writer.AppendLine("new global::TUnit.Core.PropertyMetadata");
        writer.AppendLine("{");
        
        // Manually increment indent level without calling EnsureNewLine
        var currentIndent = writer.IndentLevel;
        writer.SetIndentLevel(currentIndent + 1);
        
        writer.AppendLine($"ReflectionInfo = typeof({safeTypeNameForReflection}).GetProperty(\"{property.Name}\"),");
        writer.AppendLine($"Type = typeof({safePropertyTypeName}),");
        writer.AppendLine($"Name = \"{property.Name}\",");
        writer.AppendLine($"IsStatic = {property.IsStatic.ToString().ToLower()},");
        writer.AppendLine($"IsNullable = {property.Type.IsNullable().ToString().ToLowerInvariant()},");
        writer.AppendLine($"Getter = {GetPropertyAccessor(containingType, property)},");
        writer.AppendLine("ClassMetadata = null!,");
        writer.Append("ContainingTypeMetadata = null!");
        
        // Manually restore indent level
        writer.SetIndentLevel(currentIndent);
        writer.AppendLine();
        writer.Append("}");
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
    /// Writes an array of ParameterMetadata objects for method parameters with proper reflection info
    /// </summary>
    private static void WriteParameterMetadataArrayForMethod(ICodeWriter writer, IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
        {
            writer.Append("global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>()");
            return;
        }

        writer.AppendLine("new global::TUnit.Core.ParameterMetadata[]");
        writer.AppendLine("{");
        
        // Manually increment indent level without calling EnsureNewLine
        var currentIndent = writer.IndentLevel;
        writer.SetIndentLevel(currentIndent + 1);

        for (int i = 0; i < method.Parameters.Length; i++)
        {
            var param = method.Parameters[i];
            WriteParameterMetadata(writer, param, method);
            
            if (i < method.Parameters.Length - 1)
            {
                writer.AppendLine(",");
            }
        }

        // Manually restore indent level
        writer.SetIndentLevel(currentIndent);
        writer.AppendLine();
        writer.Append("}");
    }
    
    /// <summary>
    /// Generates an array of ParameterMetadata objects for method parameters with proper reflection info (for backward compat)
    /// </summary>
    private static string GenerateParameterMetadataArrayForMethod(IMethodSymbol method, int currentIndentLevel = 0)
    {
        var writer = new CodeWriter("", includeHeader: false).SetIndentLevel(currentIndentLevel);
        WriteParameterMetadataArrayForMethod(writer, method);
        return writer.ToString();
    }

    /// <summary>
    /// Writes an array of ParameterMetadata objects for constructor parameters with proper reflection info
    /// </summary>
    private static void WriteParameterMetadataArrayForConstructor(ICodeWriter writer, IMethodSymbol constructor, INamedTypeSymbol containingType)
    {
        if (constructor.Parameters.Length == 0)
        {
            writer.Append("global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>()");
            return;
        }

        writer.AppendLine("new global::TUnit.Core.ParameterMetadata[]");
        writer.AppendLine("{");
        
        // Manually increment indent level without calling EnsureNewLine
        var currentIndent = writer.IndentLevel;
        writer.SetIndentLevel(currentIndent + 1);

        for (int i = 0; i < constructor.Parameters.Length; i++)
        {
            var param = constructor.Parameters[i];
            WriteParameterMetadata(writer, param, constructor);
            
            if (i < constructor.Parameters.Length - 1)
            {
                writer.AppendLine(",");
            }
        }

        // Manually restore indent level
        writer.SetIndentLevel(currentIndent);
        writer.AppendLine();
        writer.Append("}");
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

        for (int i = 0; i < constructor.Parameters.Length; i++)
        {
            var param = constructor.Parameters[i];
            WriteParameterMetadata(writer, param, constructor);
            
            if (i < constructor.Parameters.Length - 1)
            {
                writer.AppendLine(",");
            }
        }

        writer.Unindent();
        writer.Append("}");

        return writer.ToString();
    }

    /// <summary>
    /// Writes an array of PropertyMetadata objects
    /// </summary>
    private static void WritePropertyMetadataArray(ICodeWriter writer, INamedTypeSymbol typeSymbol)
    {
        var properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.Name != "EqualityContract")
            .ToList();

        if (!properties.Any())
        {
            writer.Append("global::System.Array.Empty<global::TUnit.Core.PropertyMetadata>()");
            return;
        }

        writer.AppendLine("new global::TUnit.Core.PropertyMetadata[]");
        writer.AppendLine("{");
        
        // Manually increment indent level without calling EnsureNewLine
        var currentIndent = writer.IndentLevel;
        writer.SetIndentLevel(currentIndent + 1);

        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            WritePropertyMetadata(writer, prop, typeSymbol);
            
            if (i < properties.Count - 1)
            {
                writer.AppendLine(",");
            }
        }

        // Manually restore indent level
        writer.SetIndentLevel(currentIndent);
        writer.AppendLine();
        writer.Append("}");
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

        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            WritePropertyMetadata(writer, prop, typeSymbol);
            
            if (i < properties.Count - 1)
            {
                writer.AppendLine(",");
            }
        }

        writer.Unindent();
        writer.Append("}");

        return writer.ToString();
    }
}
