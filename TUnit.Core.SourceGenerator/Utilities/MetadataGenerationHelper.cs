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
    /// Generates code for creating a TestMetadata<T> instance
    /// </summary>
    public static void GenerateTestMetadata(CodeWriter writer, string metadataTypeParameter, TestMetadataGenerationArgs args)
    {
        writer.AppendLine($"var metadata = new global::TUnit.Core.TestMetadata<{metadataTypeParameter}>");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"TestName = \"{args.TestName}\",");
        writer.AppendLine($"TestClassType = {args.TestClassTypeReference},");
        writer.AppendLine($"TestMethodName = \"{args.TestMethodName}\",");
        writer.AppendLine($"Categories = {GenerateStringArray(args.Categories)},");
        writer.AppendLine($"IsSkipped = {args.IsSkipped.ToString().ToLower()},");
        writer.AppendLine($"SkipReason = {(args.SkipReason != null ? $"\"{args.SkipReason}\"" : "null")},");
        writer.AppendLine($"TimeoutMs = {args.TimeoutMs?.ToString() ?? "null"},");
        writer.AppendLine($"RetryCount = {args.RetryCount},");
        writer.AppendLine($"RepeatCount = {args.RepeatCount},");
        writer.AppendLine($"CanRunInParallel = {args.CanRunInParallel.ToString().ToLower()},");
        writer.AppendLine($"Dependencies = {args.Dependencies ?? "global::System.Array.Empty<global::TUnit.Core.TestDependency>()"},");
        writer.AppendLine($"DataSources = {args.DataSources ?? "global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>()"},");
        writer.AppendLine($"ClassDataSources = {args.ClassDataSources ?? "global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>()"},");
        writer.AppendLine($"PropertyDataSources = {args.PropertyDataSources ?? "global::System.Array.Empty<global::TUnit.Core.PropertyDataSource>()"},");
        writer.AppendLine($"InstanceFactory = {args.InstanceFactory ?? "null"},");
        writer.AppendLine($"TestInvoker = {args.TestInvoker ?? "null"},");
        writer.AppendLine($"ParameterCount = {args.ParameterCount},");
        writer.AppendLine($"ParameterTypes = {args.ParameterTypes ?? "global::System.Array.Empty<global::System.Type>()"},");
        writer.AppendLine($"TestMethodParameterTypes = {args.TestMethodParameterTypes ?? "global::System.Array.Empty<string>()"},");
        writer.AppendLine($"Hooks = {args.Hooks ?? "new global::TUnit.Core.TestHooks { BeforeClass = global::System.Array.Empty<global::TUnit.Core.HookMetadata>(), AfterClass = global::System.Array.Empty<global::TUnit.Core.HookMetadata>(), BeforeTest = global::System.Array.Empty<global::TUnit.Core.HookMetadata>(), AfterTest = global::System.Array.Empty<global::TUnit.Core.HookMetadata>() }"},");
        writer.AppendLine($"FilePath = {(args.FilePath != null ? $"@\"{args.FilePath}\"" : "null")},");
        writer.AppendLine($"LineNumber = {args.LineNumber?.ToString() ?? "null"},");
        writer.AppendLine($"MethodMetadata = {args.MethodMetadata},");
        writer.AppendLine($"GenericTypeInfo = {args.GenericTypeInfo ?? "null"},");
        writer.AppendLine($"GenericMethodInfo = {args.GenericMethodInfo ?? "null"},");
        writer.AppendLine($"GenericMethodTypeArguments = {args.GenericMethodTypeArguments ?? "null"},");
        writer.AppendLine($"AttributeFactory = {args.AttributeFactory},");
        writer.AppendLine($"PropertyInjections = {args.PropertyInjections ?? "global::System.Array.Empty<global::TUnit.Core.PropertyInjectionData>()"},");
        writer.AppendLine($"TestSessionId = \"{args.TestSessionId}\",");
        
        // Additional properties specific to TestMetadata<T>
        if (args.CreateInstance != null)
        {
            writer.AppendLine($"CreateInstance = {args.CreateInstance},");
        }
        if (args.InvokeTest != null)
        {
            writer.AppendLine($"InvokeTest = {args.InvokeTest},");
        }
        if (args.InvokeTypedTest != null)
        {
            writer.AppendLine($"InvokeTypedTest = {args.InvokeTypedTest},");
        }
        
        writer.Unindent();
        writer.AppendLine("};");
    }

    /// <summary>
    /// Generates code for creating a MethodMetadata instance
    /// </summary>
    public static string GenerateMethodMetadata(IMethodSymbol methodSymbol, string classMetadataExpression)
    {
        var writer = new CodeWriter("", includeHeader: false);
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
        writer.AppendLine($"Parameters = {GenerateParameterMetadataArrayForMethod(methodSymbol)},");
        writer.AppendLine($"Class = {classMetadataExpression}");
        
        writer.Unindent();
        writer.Append("}");
        
        return writer.ToString();
    }

    /// <summary>
    /// Generates code for creating a ClassMetadata instance with GetOrAdd pattern
    /// </summary>
    public static string GenerateClassMetadataGetOrAdd(INamedTypeSymbol typeSymbol, string? parentExpression = null)
    {
        var qualifiedName = $"{typeSymbol.ContainingAssembly.Name}:{typeSymbol.GloballyQualified()}";
        var writer = new CodeWriter("", includeHeader: false);
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
            writer.AppendLine($"Parameters = {GenerateParameterMetadataArrayForConstructor(constructor, typeSymbol)},");
        }
        else
        {
            writer.AppendLine("Parameters = global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>(),");
        }
        writer.AppendLine($"Properties = {GeneratePropertyMetadataArray(typeSymbol)},");
        writer.AppendLine($"Parent = {parentExpression ?? "null"}");
        
        writer.Unindent();
        writer.AppendLine("};");
        writer.AppendLine();
        
        // Set ClassMetadata reference on each property
        writer.AppendLine("// Set ClassMetadata reference on properties to avoid circular dependency");
        writer.AppendLine("foreach (var prop in classMetadata.Properties)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("prop.ClassMetadata = classMetadata;");
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
                return $@"typeof({containingType}).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static).FirstOrDefault(m => m.Name == ""{method.Name}"" && m.GetParameters().Length == {method.Parameters.Length})?.GetParameters()[{parameterIndex}]!";
            }
            else
            {
                // For non-generic methods, we can use GetMethod with parameter types
                var bindingFlags = method.IsStatic ? "System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static" : "System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance";
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
    Getter = {GetPropertyAccessor(containingType, property)},
    ClassMetadata = null
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
    /// Generates an array of ParameterMetadata objects
    /// </summary>
    private static string GenerateParameterMetadataArray(IEnumerable<IParameterSymbol> parameters, IMethodSymbol? containingMethod = null)
    {
        var paramList = parameters.ToList();
        if (!paramList.Any())
        {
            return "global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>()";
        }

        var writer = new CodeWriter("", includeHeader: false);
        writer.AppendLine("new global::TUnit.Core.ParameterMetadata[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var param in paramList)
        {
            writer.AppendLine($"{GenerateParameterMetadata(param, containingMethod)},");
        }

        writer.Unindent();
        writer.Append("}");
        
        return writer.ToString();
    }
    
    /// <summary>
    /// Generates an array of ParameterMetadata objects for method parameters with proper reflection info
    /// </summary>
    private static string GenerateParameterMetadataArrayForMethod(IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
        {
            return "global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>()";
        }

        var writer = new CodeWriter("", includeHeader: false);
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
    private static string GenerateParameterMetadataArrayForConstructor(IMethodSymbol constructor, INamedTypeSymbol containingType)
    {
        if (constructor.Parameters.Length == 0)
        {
            return "global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>()";
        }

        var writer = new CodeWriter("", includeHeader: false);
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
    private static string GeneratePropertyMetadataArray(INamedTypeSymbol typeSymbol)
    {
        var properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.Name != "EqualityContract")
            .ToList();

        if (!properties.Any())
        {
            return "global::System.Array.Empty<global::TUnit.Core.PropertyMetadata>()";
        }

        var writer = new CodeWriter("", includeHeader: false);
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

    /// <summary>
    /// Generates a string array expression
    /// </summary>
    private static string GenerateStringArray(string[]? values)
    {
        if (values == null || values.Length == 0)
        {
            return "global::System.Array.Empty<string>()";
        }

        var items = string.Join(", ", values.Select(v => $"\"{v}\""));
        return $"new string[] {{ {items} }}";
    }
}

/// <summary>
/// Arguments for generating TestMetadata
/// </summary>
internal class TestMetadataGenerationArgs
{
    public required string TestName { get; init; }
    public required string TestClassTypeReference { get; init; }
    public required string TestMethodName { get; init; }
    public string[]? Categories { get; init; }
    public bool IsSkipped { get; init; }
    public string? SkipReason { get; init; }
    public int? TimeoutMs { get; init; }
    public int RetryCount { get; init; }
    public int RepeatCount { get; init; } = 1;
    public bool CanRunInParallel { get; init; } = true;
    public string? Dependencies { get; init; }
    public string? DataSources { get; init; }
    public string? ClassDataSources { get; init; }
    public string? PropertyDataSources { get; init; }
    public string? InstanceFactory { get; init; }
    public string? TestInvoker { get; init; }
    public int ParameterCount { get; init; }
    public string? ParameterTypes { get; init; }
    public string? TestMethodParameterTypes { get; init; }
    public string? Hooks { get; init; }
    public string? FilePath { get; init; }
    public int? LineNumber { get; init; }
    public required string MethodMetadata { get; init; }
    public string? GenericTypeInfo { get; init; }
    public string? GenericMethodInfo { get; init; }
    public string? GenericMethodTypeArguments { get; init; }
    public required string AttributeFactory { get; init; }
    public string? PropertyInjections { get; init; }
    public required string TestSessionId { get; init; }
    
    // Additional properties specific to TestMetadata<T>
    public string? CreateInstance { get; init; }
    public string? InvokeTest { get; init; }
    public string? InvokeTypedTest { get; init; }
}