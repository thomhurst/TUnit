using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Utilities;

namespace TUnit.Core.SourceGenerator;

/// <summary>
/// Helper methods for generating C# code strings from Roslyn symbols and metadata.
/// </summary>
internal static class CodeGenerationHelpers
{
    /// <summary>
    /// Generates C# code for a ParameterMetadata array from method parameters.
    /// </summary>
    public static string GenerateParameterMetadataArray(IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
        {
            return "System.Array.Empty<global::TUnit.Core.ParameterMetadata>()";
        }

        using var writer = new CodeWriter("", includeHeader: false);
        writer.SetIndentLevel(2);
        using (writer.BeginArrayInitializer("new global::TUnit.Core.ParameterMetadata[]"))
        {
            foreach (var param in method.Parameters)
            {
                var parameterIndex = method.Parameters.IndexOf(param);
                // Handle type parameters and types containing type parameters
                var containsTypeParam = ContainsTypeParameter(param.Type);
                var typeForConstructor = containsTypeParam ? "object" : param.Type.GloballyQualified();
                
                // Debug: Add comment to show what's happening
                if (containsTypeParam)
                {
                    writer.AppendLine($"// Parameter '{param.Name}' contains type parameters, using typeof(object)");
                }
                
                using (writer.BeginObjectInitializer($"new global::TUnit.Core.ParameterMetadata(typeof({typeForConstructor}))", ","))
                {
                    writer.AppendLine($"Name = \"{param.Name}\",");
                    writer.AppendLine($"TypeReference = {GenerateTypeReference(param.Type)},");
                    var paramTypesArray = GenerateParameterTypesArray(method);
                    if (paramTypesArray == "null")
                    {
                        // For methods with generic parameters, use GetMethods and find by name
                        writer.AppendLine($"ReflectionInfo = typeof({method.ContainingType.GloballyQualified()}).GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == \"{method.Name}\" && m.GetParameters().Length == {method.Parameters.Length})?.GetParameters()[{parameterIndex}]");
                    }
                    else
                    {
                        writer.AppendLine($"ReflectionInfo = typeof({method.ContainingType.GloballyQualified()}).GetMethod(\"{method.Name}\", BindingFlags.Public | BindingFlags.Instance, null, {paramTypesArray}, null)!.GetParameters()[{parameterIndex}]");
                    }
                }
            }
        }
        return writer.ToString().TrimEnd(); // Trim trailing newline for inline use
    }

    /// <summary>
    /// Generates direct instantiation code for attributes.
    /// </summary>
    public static string GenerateAttributeInstantiation(AttributeData attr)
    {
        var typeName = attr.AttributeClass!.GloballyQualified();
        using var writer = new CodeWriter("", includeHeader: false);
        writer.SetIndentLevel(1);
        writer.Append($"new {typeName}(");

        // Add constructor arguments
        if (attr.ConstructorArguments.Length > 0)
        {
            var argStrings = new List<string>();

            for (var i = 0; i < attr.ConstructorArguments.Length; i++)
            {
                var arg = attr.ConstructorArguments[i];

                // Check if this is a params array parameter
                if (i == attr.ConstructorArguments.Length - 1 && IsParamsArrayArgument(attr, i))
                {
                    // For params arrays, expand the array elements
                    if (arg.Kind == TypedConstantKind.Array)
                    {
                        // Check if the array is initialized before accessing it
                        if (!arg.Values.IsDefault)
                        {
                            var elements = arg.Values.Select(TypedConstantParser.GetRawTypedConstantValue);
                            argStrings.AddRange(elements);
                        }
                        // If Values is default/uninitialized, don't add any arguments for the params array
                    }
                    else
                    {
                        argStrings.Add(TypedConstantParser.GetRawTypedConstantValue(arg));
                    }
                }
                else
                {
                    argStrings.Add(TypedConstantParser.GetRawTypedConstantValue(arg));
                }
            }

            writer.Append(string.Join(", ", argStrings));
        }

        writer.Append(")");

        // Add named arguments
        if (attr.NamedArguments.Length > 0)
        {
            writer.Append(" { ");
            var namedArgs = attr.NamedArguments.Select(na => $"{na.Key} = {TypedConstantParser.GetRawTypedConstantValue(na.Value)}");
            writer.Append(string.Join(", ", namedArgs));
            writer.Append(" }");
        }

        return writer.ToString().Trim();
    }

    /// <summary>
    /// Determines if an argument is for a params array parameter.
    /// </summary>
    private static bool IsParamsArrayArgument(AttributeData attr, int argumentIndex)
    {
        // For known TUnit attributes with params, handle them explicitly
        var typeName = attr.AttributeClass!.GloballyQualified();

        // ArgumentsAttribute and InlineDataAttribute use params object[]
        if (typeName == "global::TUnit.Core.ArgumentsAttribute" ||
            typeName == "global::TUnit.Core.InlineDataAttribute")
        {
            return true;
        }

        // For other attributes, we'd need to check the constructor's parameter attributes
        // but that's more complex and not needed for TUnit's current attributes
        return false;
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
    /// Gets a safe type name for use in typeof() expressions.
    /// Returns "object" only for actual type parameters or types containing them.
    /// Returns open generic forms (e.g., List<>) for generic type definitions.
    /// </summary>
    

    /// <summary>
    /// Generates C# code for PropertyMetadata array from class properties.
    /// </summary>
    public static string GeneratePropertyMetadataArray(INamedTypeSymbol typeSymbol)
    {
        var properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
            .ToList();

        if (properties.Count == 0)
        {
            return "System.Array.Empty<global::TUnit.Core.PropertyMetadata>()";
        }

        using var writer = new CodeWriter("", includeHeader: false);
        writer.SetIndentLevel(2);
        using (writer.BeginArrayInitializer("new global::TUnit.Core.PropertyMetadata[]"))
        {
            foreach (var prop in properties)
            {
                using (writer.BeginObjectInitializer("new global::TUnit.Core.PropertyMetadata", ","))
                {
                    writer.AppendLine($"Name = \"{prop.Name}\",");
                    writer.AppendLine($"Type = typeof({prop.Type.GloballyQualified()}),");
                    writer.AppendLine($"ReflectionInfo = typeof({typeSymbol.GloballyQualified()}).GetProperty(\"{prop.Name}\"),");
                    writer.AppendLine("IsStatic = false,");
                    writer.AppendLine($"Getter = obj => ((({typeSymbol.GloballyQualified()})obj).{prop.Name}),");
                    writer.AppendLine("ClassMetadata = null");
                }
            }
        }
        return writer.ToString().TrimEnd(); // Trim trailing newline for inline use
    }

    /// <summary>
    /// Generates C# code for ConstructorMetadata array from class constructors.
    /// </summary>

    /// <summary>
    /// Generates C# code for class-level data source providers.
    /// </summary>
    public static string GenerateClassDataSourceProviders(INamedTypeSymbol typeSymbol)
    {
        var dataSourceAttributes = typeSymbol.GetAttributes()
            .Where(attr => IsDataSourceAttribute(attr))
            .ToList();

        if (dataSourceAttributes.Count == 0)
        {
            return "System.Array.Empty<global::TUnit.Core.TestDataSource>()";
        }

        using var writer = new CodeWriter("", includeHeader: false);
        writer.SetIndentLevel(2);
        using (writer.BeginArrayInitializer("new global::TUnit.Core.TestDataSource[]"))
        {
            foreach (var attr in dataSourceAttributes)
            {
                var providerCode = GenerateDataSourceProvider(attr, typeSymbol);
                if (!string.IsNullOrEmpty(providerCode))
                {
                    writer.AppendLine($"{providerCode},");
                }
            }
        }
        return writer.ToString().TrimEnd(); // Trim trailing newline for inline use
    }

    /// <summary>
    /// Generates C# code for method-level data source providers.
    /// </summary>
    public static string GenerateMethodDataSourceProviders(IMethodSymbol methodSymbol)
    {
        var dataSourceAttributes = methodSymbol.GetAttributes()
            .Where(attr => IsDataSourceAttribute(attr))
            .ToList();

        // Also check method parameters for data attributes
        foreach (var param in methodSymbol.Parameters)
        {
            dataSourceAttributes.AddRange(param.GetAttributes().Where(IsDataSourceAttribute));
        }

        if (dataSourceAttributes.Count == 0)
        {
            return "System.Array.Empty<global::TUnit.Core.TestDataSource>()";
        }

        using var writer = new CodeWriter("", includeHeader: false);
        writer.SetIndentLevel(2);
        using (writer.BeginArrayInitializer("new global::TUnit.Core.TestDataSource[]"))
        {
            foreach (var attr in dataSourceAttributes)
            {
                var providerCode = GenerateDataSourceProvider(attr, methodSymbol.ContainingType);
                if (!string.IsNullOrEmpty(providerCode))
                {
                    writer.AppendLine($"{providerCode},");
                }
            }
        }
        return writer.ToString().TrimEnd(); // Trim trailing newline for inline use
    }

    /// <summary>
    /// Determines if an attribute is a data source attribute.
    /// </summary>
    private static bool IsDataSourceAttribute(AttributeData attr)
    {
        var fullName = attr.AttributeClass!.GloballyQualified();
        return fullName == "TUnit.Core.ArgumentsAttribute" ||
               fullName == "TUnit.Core.InlineDataAttribute" ||
               fullName == "TUnit.Core.MethodDataSourceAttribute" ||
               fullName == "TUnit.Core.ClassDataSourceAttribute" ||
               fullName == "TUnit.Core.PropertyDataSourceAttribute" ||
               attr.AttributeClass!.AllInterfaces.Any(i =>
                   i.GloballyQualified() == "TUnit.Core.Interfaces.IDataSourceAttribute");
    }

    /// <summary>
    /// Generates a data source provider instance based on the attribute type.
    /// </summary>
    private static string GenerateDataSourceProvider(AttributeData attr, INamedTypeSymbol containingType)
    {
        var fullName = attr.AttributeClass!.GloballyQualified();

        switch (fullName)
        {
            case "TUnit.Core.ArgumentsAttribute":
            case "TUnit.Core.InlineDataAttribute":
                return GenerateInlineDataProvider(attr);

            case "TUnit.Core.MethodDataSourceAttribute":
                return GenerateMethodDataSourceProvider(attr, containingType);

            case "TUnit.Core.ClassDataSourceAttribute":
                return GenerateClassDataSourceProvider(attr);

            case "TUnit.Core.PropertyDataSourceAttribute":
                return GeneratePropertyDataSourceProvider(attr, containingType);

            default:
                // For custom IDataSourceAttribute implementations
                return GenerateCustomDataProvider(attr);
        }
    }

    private static string GenerateInlineDataProvider(AttributeData attr)
    {
        using var writer = new CodeWriter("", includeHeader: false);
        writer.Append("new global::TUnit.Core.StaticTestDataSource(new object?[][] { new object?[] { ");

        var args = attr.ConstructorArguments.Select(TypedConstantParser.GetRawTypedConstantValue).ToList();
        writer.Append(string.Join(", ", args));
        writer.Append(" } })");
        return writer.ToString().Trim();
    }

    private static string GenerateMethodDataSourceProvider(AttributeData attr, INamedTypeSymbol containingType)
    {
        if (attr.ConstructorArguments.Length == 0)
        {
            return "";
        }

        var methodName = attr.ConstructorArguments[0].Value?.ToString() ?? "";
        var isShared = attr.NamedArguments.FirstOrDefault(na => na.Key == "Shared").Value.Value as bool? ?? false;

        // Try to determine if this can be optimized for AOT
        var method = FindDataSourceMethod(methodName, containingType);
        if (method != null && ShouldUseAotOptimizedDataSource(method))
        {
            // Generate code that uses a more AOT-friendly approach
            return GenerateAotOptimizedDataSource(methodName, containingType, isShared);
        }

        // Fall back to DynamicTestDataSource for complex cases
        return $"new global::TUnit.Core.DynamicTestDataSource({isShared.ToString().ToLowerInvariant()}) {{ SourceType = typeof({containingType.GloballyQualified()}), SourceMemberName = \"{methodName}\" }}";
    }

    private static string GenerateParameterTypesArray(IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
        {
            return "System.Type.EmptyTypes";
        }

        // Check if any parameter contains type parameters
        if (method.Parameters.Any(p => ContainsTypeParameter(p.Type)))
        {
            // Return null to indicate that parameter type matching should be done at runtime
            return "null";
        }

        var parameterTypes = method.Parameters
            .Select(p => $"typeof({p.Type.GloballyQualified()})")
            .ToArray();

        return $"new System.Type[] {{ {string.Join(", ", parameterTypes)} }}";
    }

    private static string GenerateClassDataSourceProvider(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length == 0)
        {
            return "";
        }

        if (attr.ConstructorArguments[0].Value is not ITypeSymbol dataSourceType)
        {
            return "";
        }

        var isShared = attr.NamedArguments.FirstOrDefault(na => na.Key == "Shared").Value.Value as bool? ?? false;

        // Check if we can use AOT-friendly approach for class data sources
        if (dataSourceType is INamedTypeSymbol namedType)
        {
            var getDataMethod = FindDataSourceMethod("GetData", namedType);
            if (getDataMethod != null && ShouldUseAotOptimizedDataSource(getDataMethod))
            {
                return GenerateAotOptimizedClassDataSource("GetData", namedType, isShared);
            }
        }

        return $"new global::TUnit.Core.DynamicTestDataSource({isShared.ToString().ToLowerInvariant()}) {{ SourceType = typeof({dataSourceType.GloballyQualified()}), SourceMemberName = \"GetData\" }}";
    }

    private static string GeneratePropertyDataSourceProvider(AttributeData attr, INamedTypeSymbol containingType)
    {
        if (attr.ConstructorArguments.Length == 0)
        {
            return "";
        }

        var propertyName = attr.ConstructorArguments[0].Value?.ToString() ?? "";
        var isShared = attr.NamedArguments.FirstOrDefault(na => na.Key == "Shared").Value.Value as bool? ?? false;

        // Check if we can use AOT-friendly approach for property data sources
        var property = containingType.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault();
        if (property != null && ShouldUseAotOptimizedPropertyDataSource(property))
        {
            return GenerateAotOptimizedPropertyDataSource(propertyName, containingType, isShared);
        }

        return $"new global::TUnit.Core.DynamicTestDataSource({isShared.ToString().ToLowerInvariant()}) {{ SourceType = typeof({containingType.GloballyQualified()}), SourceMemberName = \"{propertyName}\" }}";
    }

    private static string GenerateCustomDataProvider(AttributeData attr)
    {
        // For custom data attributes, we need to be more careful about AOT compatibility
        // Most custom data attributes will require dynamic code, so they should use DynamicTestDataSource
        // with appropriate warning attributes on the attribute class itself
        return $"new global::TUnit.Core.DynamicTestDataSource(false) {{ SourceType = typeof({attr.AttributeClass!.GloballyQualified()}), SourceMemberName = \"GetData\" }}";
    }

    /// <summary>
    /// Extracts timeout value from timeout attributes on method or class.
    /// </summary>
    public static string ExtractTimeout(IMethodSymbol methodSymbol)
    {
        // Check method first, then class
        var timeoutAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass!.GloballyQualified() == "TUnit.Core.TimeoutAttribute") ??
            methodSymbol.ContainingType.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass!.GloballyQualified() == "TUnit.Core.TimeoutAttribute");

        if (timeoutAttr == null || timeoutAttr.ConstructorArguments.Length == 0)
        {
            return "null";
        }

        var timeoutValue = timeoutAttr.ConstructorArguments[0].Value;
        if (timeoutValue is int milliseconds)
        {
            return $"System.TimeSpan.FromMilliseconds({milliseconds})";
        }

        return "null";
    }

    /// <summary>
    /// Checks if test should be skipped and extracts skip reason.
    /// </summary>
    public static (bool isSkipped, string skipReason) ExtractSkipInfo(IMethodSymbol methodSymbol)
    {
        // Check method first, then class
        var skipAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass!.GloballyQualified() == "TUnit.Core.SkipAttribute") ??
            methodSymbol.ContainingType.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass!.GloballyQualified() == "TUnit.Core.SkipAttribute");

        if (skipAttr == null)
        {
            return (false, "null");
        }

        var reason = skipAttr.ConstructorArguments.Length > 0
            ? TypedConstantParser.GetRawTypedConstantValue(skipAttr.ConstructorArguments[0])
            : "\"Test skipped\"";

        return (true, reason);
    }

    /// <summary>
    /// Extracts repeat count from repeat attributes.
    /// </summary>
    public static int ExtractRepeatCount(IMethodSymbol methodSymbol)
    {
        var repeatAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass!.Name == "RepeatAttribute");

        if (repeatAttr == null || repeatAttr.ConstructorArguments.Length == 0)
        {
            return 0;
        }

        if (repeatAttr.ConstructorArguments[0].Value is int count and > 0)
        {
            return count;
        }

        return 0;
    }

    /// <summary>
    /// Generates all test-related attributes for the TestMetadata.Attributes field.
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
            return "System.Array.Empty<System.Attribute>()";
        }

        // Generate as a single line array to avoid CS8802 parser issues
        using var writer = new CodeWriter("", includeHeader: false);

        // Generate inline array to avoid parser issues
        using (writer.BeginArrayInitializer("new System.Attribute[]", terminator: ""))
        {
            var attributeStrings = new List<string>();
            foreach (var attr in allAttributes)
            {
                // Use unified approach for all attributes
                attributeStrings.Add(GenerateAttributeInstantiation(attr));
            }
            writer.Append(string.Join(", ", attributeStrings));
        }

        return writer.ToString().Trim();
    }

    /// <summary>
    /// Generates C# code to create a TypeReference from an ITypeSymbol.
    /// </summary>
    public static string GenerateTypeReference(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is ITypeParameterSymbol typeParameter)
        {
            // This is a generic parameter (e.g., T, TKey)
            var position = GetGenericParameterPosition(typeParameter);
            var isMethodParameter = typeParameter.DeclaringMethod != null;
            return $@"global::TUnit.Core.TypeReference.CreateGenericParameter({position}, {(isMethodParameter ? "true" : "false")}, ""{typeParameter.Name}"")";
        }

        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            // This is an array type
            var elementTypeRef = GenerateTypeReference(arrayType.ElementType);
            return $@"global::TUnit.Core.TypeReference.CreateArray({elementTypeRef}, {arrayType.Rank})";
        }

        if (typeSymbol is IPointerTypeSymbol pointerType)
        {
            // This is a pointer type
            var elementTypeRef = GenerateTypeReference(pointerType.PointedAtType);
            return $@"new global::TUnit.Core.TypeReference {{ IsPointer = true, ElementType = {elementTypeRef} }}";
        }

        if (typeSymbol is INamedTypeSymbol namedType)
        {
            if (namedType is { IsGenericType: true, IsUnboundGenericType: false })
            {
                // This is a constructed generic type (e.g., List<int>, Dictionary<string, T>)
                var genericDef = GetGenericTypeDefinitionName(namedType);
                var genericArgs = namedType.TypeArguments.Select(GenerateTypeReference).ToArray();

                using var writer = new CodeWriter("", includeHeader: false);
                writer.SetIndentLevel(1);
                writer.Append($@"global::TUnit.Core.TypeReference.CreateConstructedGeneric(""{genericDef}""");
                foreach (var arg in genericArgs)
                {
                    writer.Append($", {arg}");
                }
                writer.Append(")");
                return writer.ToString().Trim();
            }
        }

        // Regular concrete type
        var assemblyQualifiedName = GetAssemblyQualifiedName(typeSymbol);
        return $@"global::TUnit.Core.TypeReference.CreateConcrete(""{assemblyQualifiedName}"")";
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

    private static string GetGenericTypeDefinitionName(INamedTypeSymbol namedType)
    {
        // Get the unbound generic type (e.g., List`1)
        var unboundType = namedType.ConstructUnboundGenericType();
        return GetAssemblyQualifiedName(unboundType);
    }

    private static string GetAssemblyQualifiedName(ITypeSymbol typeSymbol)
    {
        // Build assembly qualified name
        var typeName = typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithoutGlobalPrefix);

        // For well-known types, use simplified names
        if (typeSymbol.ContainingAssembly.Name == "System.Private.CoreLib" ||
            typeSymbol.ContainingAssembly.Name == "mscorlib")
        {
            return $"{typeName}, System.Private.CoreLib";
        }

        return $"{typeName}, {typeSymbol.ContainingAssembly.Name}";
    }

    #region Compile-Time Data Source Resolution

    /// <summary>
    /// Finds a data source method in the containing type.
    /// </summary>
    private static IMethodSymbol? FindDataSourceMethod(string methodName, INamedTypeSymbol containingType)
    {
        return containingType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.Parameters.Length == 0);
    }

    /// <summary>
    /// Determines if a method should use AOT-optimized data source generation.
    /// </summary>
    private static bool ShouldUseAotOptimizedDataSource(IMethodSymbol method)
    {
        // For now, we'll use a conservative approach and only optimize static methods
        // with simple return types that are clearly data sources
        if (!method.IsStatic)
        {
            return false;
        }
        if (method.Parameters.Length > 0)
        {
            return false;
        }

        var returnType = method.ReturnType;

        // Check for common data source return types
        if (returnType is INamedTypeSymbol namedType)
        {
            var typeString = namedType.ToDisplayString();

            // Look for IEnumerable patterns that are commonly used for test data
            return typeString.Contains("IEnumerable<") ||
                   typeString.Contains("ICollection<") ||
                   typeString.Contains("List<") ||
                   typeString == "object[][]" ||
                   typeString.Contains("object[]");
        }

        return false;
    }

    /// <summary>
    /// Generates AOT-optimized data source code that avoids reflection.
    /// </summary>
    private static string GenerateAotOptimizedDataSource(string methodName, INamedTypeSymbol containingType, bool isShared)
    {
        // For AOT compatibility, we'll use a compile-time generated data source
        // that calls the method directly without reflection
        return $"new global::TUnit.Core.AotFriendlyTestDataSource({isShared.ToString().ToLowerInvariant()}) {{ " +
               $"MethodInvoker = () => {containingType.GloballyQualified()}.{methodName}(), " +
               $"SourceType = typeof({containingType.GloballyQualified()}), " +
               $"SourceMemberName = \"{methodName}\" }}";
    }

    /// <summary>
    /// Generates AOT-optimized class data source code that avoids reflection.
    /// </summary>
    private static string GenerateAotOptimizedClassDataSource(string methodName, INamedTypeSymbol containingType, bool isShared)
    {
        // For class data sources, instantiate the class and call the method
        return $"new global::TUnit.Core.AotFriendlyTestDataSource({isShared.ToString().ToLowerInvariant()}) {{ " +
               $"MethodInvoker = () => new {containingType.GloballyQualified()}().{methodName}(), " +
               $"SourceType = typeof({containingType.GloballyQualified()}), " +
               $"SourceMemberName = \"{methodName}\" }}";
    }

    /// <summary>
    /// Generates AOT-optimized property data source code that avoids reflection.
    /// </summary>
    private static string GenerateAotOptimizedPropertyDataSource(string propertyName, INamedTypeSymbol containingType, bool isShared)
    {
        // For property data sources, instantiate the class and access the property
        return $"new global::TUnit.Core.AotFriendlyTestDataSource({isShared.ToString().ToLowerInvariant()}) {{ " +
               $"MethodInvoker = () => new {containingType.GloballyQualified()}().{propertyName}, " +
               $"SourceType = typeof({containingType.GloballyQualified()}), " +
               $"SourceMemberName = \"{propertyName}\" }}";
    }

    /// <summary>
    /// Determines if a property should use AOT-optimized data source generation.
    /// </summary>
    private static bool ShouldUseAotOptimizedPropertyDataSource(IPropertySymbol property)
    {
        // For properties, check if they are static and have suitable return types
        if (!property.IsStatic)
        {
            // For instance properties, check if the containing type has a parameterless constructor
            var containingType = property.ContainingType;
            var hasParameterlessConstructor = containingType.Constructors.Any(c => c.Parameters.Length == 0);
            if (!hasParameterlessConstructor)
            {
                return false;
            }
        }

        var returnType = property.Type;

        if (returnType is INamedTypeSymbol namedType)
        {
            var typeString = namedType.ToDisplayString();

            // Look for common data source return types
            return typeString.Contains("IEnumerable<") ||
                   typeString.Contains("ICollection<") ||
                   typeString.Contains("List<") ||
                   typeString == "object[][]" ||
                   typeString.Contains("object[]");
        }

        return false;
    }

    #endregion
}
