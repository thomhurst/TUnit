using System.Collections.Immutable;
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
    /// Generates C# code for a ParameterMetadata array from method parameters.
    /// </summary>
    public static string GenerateParameterMetadataArray(IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
        {
            return "System.Array.Empty<global::TUnit.Core.ParameterMetadata>()";
        }

        using var writer = new CodeWriter("", includeHeader: false);
        for (var i = 0; i < 2; i++) { writer._indentLevel++; }
        writer.Append("new global::TUnit.Core.ParameterMetadata[] {");
        writer.AppendLine();

        foreach (var param in method.Parameters)
        {
            var parameterIndex = method.Parameters.IndexOf(param);
            var typeForConstructor = ContainsTypeParameter(param.Type) ? "object" : param.Type.GloballyQualified();
            writer.AppendLine($"new global::TUnit.Core.ParameterMetadata(typeof({typeForConstructor}))");
            writer.AppendLine("{");
            writer.AppendLine($"Name = \"{param.Name}\",");
            writer.AppendLine($"TypeReference = {GenerateTypeReference(param.Type)},");
            writer.AppendLine($"Attributes = {GenerateAttributeMetadataArray(param.GetAttributes())},");
            writer.AppendLine($"ReflectionInfo = typeof({method.ContainingType.GloballyQualified()}).GetMethod(\"{method.Name}\", BindingFlags.Public | BindingFlags.Instance).GetParameters()[{parameterIndex}]");
            writer.AppendLine("},");
        }

        writer.AppendLine("}");
        return writer.ToString().TrimEnd(); // Trim trailing newline for inline use
    }

    /// <summary>
    /// Generates C# code for an AttributeMetadata array from attributes.
    /// </summary>
    public static string GenerateAttributeMetadataArray(ImmutableArray<AttributeData> attributes)
    {
        var relevantAttributes = attributes
            .Where(attr => !IsCompilerGeneratedAttribute(attr))
            .ToList();

        if (relevantAttributes.Count == 0)
        {
            return "System.Array.Empty<global::TUnit.Core.AttributeMetadata>()";
        }

        using var writer = new CodeWriter("", includeHeader: false);
        for (var i = 0; i < 2; i++) { writer._indentLevel++; }
        writer.AppendLine("new global::TUnit.Core.AttributeMetadata[]");
        writer.AppendLine("{");

        foreach (var attr in relevantAttributes)
        {
            var attributeCode = GenerateAttributeMetadata(attr);
            if (!string.IsNullOrEmpty(attributeCode))
            {
                writer.Append(attributeCode);
            }
        }

        writer.AppendLine("}");
        return writer.ToString().TrimEnd();
    }

    /// <summary>
    /// Generates C# code for a single AttributeMetadata.
    /// </summary>
    private static string GenerateAttributeMetadata(AttributeData attr)
    {
        using var writer = new CodeWriter("", includeHeader: false);
        for (var i = 0; i < 3; i++)
        {
            writer._indentLevel++; // Start with indent level 3 for nested objects
        }

        // For known TUnit attributes, generate direct instantiation
        if (IsKnownTUnitAttribute(attr))
        {
            writer.AppendLine("new global::TUnit.Core.AttributeMetadata");
            writer.AppendLine("{");
            writer.AppendLine($"Instance = {GenerateAttributeInstantiation(attr)},");
            writer.AppendLine("TargetElement = global::TUnit.Core.TestAttributeTarget.Method,");
            writer.AppendLine("ConstructorArguments = null,");
            writer.AppendLine("NamedArguments = null");
            writer.AppendLine("},");
        }
        else
        {
            // For unknown attributes, generate runtime construction info
            writer.AppendLine("new global::TUnit.Core.AttributeMetadata");
            writer.AppendLine("{");
            writer.AppendLine("Instance = global::TUnit.Core.Helpers.RuntimeAttributeHelper.CreateAttribute(");
            writer.AppendLine($"typeof({attr.AttributeClass!.GloballyQualified()}),");
            writer.AppendLine($"{GenerateConstructorArgumentsArray(attr)},");
            writer.AppendLine($"{GenerateNamedArgumentsDictionary(attr)}),");
            writer.AppendLine("TargetElement = global::TUnit.Core.TestAttributeTarget.Method,");
            writer.AppendLine("ConstructorArguments = null,");
            writer.AppendLine("NamedArguments = null");
            writer.AppendLine("},");
        }

        return writer.ToString();
    }

    /// <summary>
    /// Determines if an attribute is a known TUnit attribute that we can safely instantiate.
    /// </summary>
    private static bool IsKnownTUnitAttribute(AttributeData attr)
    {
        var fullName = attr.AttributeClass!.GloballyQualified();
        return fullName.StartsWith("TUnit.Core.") && (
            fullName.EndsWith("TestAttribute") ||
            fullName.EndsWith("TimeoutAttribute") ||
            fullName.EndsWith("SkipAttribute") ||
            fullName.EndsWith("RepeatAttribute") ||
            fullName.EndsWith("ArgumentsAttribute") ||
            fullName.EndsWith("InlineDataAttribute"));
    }

    /// <summary>
    /// Generates direct instantiation code for known TUnit attributes.
    /// </summary>
    private static string GenerateAttributeInstantiation(AttributeData attr)
    {
        var typeName = attr.AttributeClass!.GloballyQualified();
        using var writer = new CodeWriter("", includeHeader: false);
        for (var i = 0; i < 1; i++) { writer._indentLevel++; }
        writer.Append($"new {typeName}(");

        // Add constructor arguments
        if (attr.ConstructorArguments.Length > 0)
        {
            var args = attr.ConstructorArguments.Select(TypedConstantParser.GetRawTypedConstantValue);
            writer.Append(string.Join(", ", args));
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
    /// Generates an array of constructor arguments for runtime attribute construction.
    /// </summary>
    private static string GenerateConstructorArgumentsArray(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length == 0)
        {
            return "System.Array.Empty<object?>()";
        }

        var args = attr.ConstructorArguments.Select(TypedConstantParser.GetRawTypedConstantValue);
        return $"new object?[] {{ {string.Join(", ", args)} }}";
    }

    /// <summary>
    /// Generates a dictionary of named arguments for runtime attribute construction.
    /// </summary>
    private static string GenerateNamedArgumentsDictionary(AttributeData attr)
    {
        if (attr.NamedArguments.Length == 0)
        {
            return "null";
        }

        using var writer = new CodeWriter("", includeHeader: false);
        for (var i = 0; i < 1; i++) { writer._indentLevel++; }
        writer.Append("new System.Collections.Generic.Dictionary<string, object?> { ");
        foreach (var na in attr.NamedArguments)
        {
            writer.Append($"{{ \"{na.Key}\", {TypedConstantParser.GetRawTypedConstantValue(na.Value)} }}, ");
        }
        writer.Append("}");
        return writer.ToString().Trim();
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

    private static bool ContainsTypeParameter(ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol)
        {
            return true;
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return ContainsTypeParameter(arrayType.ElementType);
        }

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            return namedType.TypeArguments.Any(ContainsTypeParameter);
        }

        return false;
    }

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
        for (var i = 0; i < 2; i++) { writer._indentLevel++; }
        writer.Append("new global::TUnit.Core.PropertyMetadata[] {");
        writer.AppendLine();

        foreach (var prop in properties)
        {
            writer.AppendLine("new global::TUnit.Core.PropertyMetadata");
            writer.AppendLine("{");
            writer.AppendLine($"Name = \"{prop.Name}\",");
            writer.AppendLine($"Type = typeof({prop.Type.GloballyQualified()}),");
            writer.AppendLine($"ReflectionInfo = typeof({typeSymbol.GloballyQualified()}).GetProperty(\"{prop.Name}\"),");
            writer.AppendLine("IsStatic = false,");
            writer.AppendLine($"Getter = obj => ((({typeSymbol.GloballyQualified()})obj).{prop.Name}),");
            writer.AppendLine($"Attributes = {GenerateAttributeMetadataArray(prop.GetAttributes())}");
            writer.AppendLine("},");
        }

        writer.AppendLine("}");
        return writer.ToString().TrimEnd(); // Trim trailing newline for inline use
    }

    /// <summary>
    /// Generates C# code for ConstructorMetadata array from class constructors.
    /// </summary>
    public static string GenerateConstructorMetadataArray(INamedTypeSymbol typeSymbol)
    {
        var constructors = typeSymbol.Constructors
            .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        if (constructors.Count == 0)
        {
            return "System.Array.Empty<global::TUnit.Core.ConstructorMetadata>()";
        }

        using var writer = new CodeWriter("", includeHeader: false);
        for (var i = 0; i < 2; i++) { writer._indentLevel++; }
        writer.Append("new global::TUnit.Core.ConstructorMetadata[] {");
        writer.AppendLine();

        foreach (var ctor in constructors)
        {
            writer.AppendLine("new global::TUnit.Core.ConstructorMetadata");
            writer.AppendLine("{");
            writer.AppendLine($"Type = typeof({typeSymbol.GloballyQualified()}),");
            writer.AppendLine("Name = \".ctor\",");
            writer.AppendLine($"Parameters = {GenerateParameterMetadataArray(ctor)},");
            writer.AppendLine($"Attributes = {GenerateAttributeMetadataArray(ctor.GetAttributes())},");
            writer.AppendLine("IsStatic = false,");
            writer.AppendLine($"IsPublic = {(ctor.DeclaredAccessibility == Accessibility.Public ? "true" : "false")},");
            writer.AppendLine($"IsPrivate = {(ctor.DeclaredAccessibility == Accessibility.Private ? "true" : "false")},");
            writer.AppendLine($"IsProtected = {(ctor.DeclaredAccessibility == Accessibility.Protected ? "true" : "false")},");
            writer.AppendLine($"IsInternal = {(ctor.DeclaredAccessibility == Accessibility.Internal ? "true" : "false")}");
            writer.AppendLine("},");
        }

        writer.AppendLine("}");
        return writer.ToString().TrimEnd(); // Trim trailing newline for inline use
    }

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
            return "System.Array.Empty<global::TUnit.Core.IDataSourceProvider>()";
        }

        using var writer = new CodeWriter("", includeHeader: false);
        for (var i = 0; i < 2; i++) { writer._indentLevel++; }
        writer.Append("new global::TUnit.Core.IDataSourceProvider[] {");
        writer.AppendLine();

        foreach (var attr in dataSourceAttributes)
        {
            var providerCode = GenerateDataSourceProvider(attr, typeSymbol);
            if (!string.IsNullOrEmpty(providerCode))
            {
                writer.AppendLine($"{providerCode},");
            }
        }

        writer.AppendLine("}");
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
            return "System.Array.Empty<global::TUnit.Core.IDataSourceProvider>()";
        }

        using var writer = new CodeWriter("", includeHeader: false);
        for (var i = 0; i < 2; i++) { writer._indentLevel++; }
        writer.Append("new global::TUnit.Core.IDataSourceProvider[] {");
        writer.AppendLine();

        foreach (var attr in dataSourceAttributes)
        {
            var providerCode = GenerateDataSourceProvider(attr, methodSymbol.ContainingType);
            if (!string.IsNullOrEmpty(providerCode))
            {
                writer.AppendLine($"{providerCode},");
            }
        }

        writer.AppendLine("}");
        return writer.ToString().TrimEnd(); // Trim trailing newline for inline use
    }

    /// <summary>
    /// Generates C# code for property data source providers dictionary.
    /// </summary>
    public static string GeneratePropertyDataSourceDictionary(INamedTypeSymbol typeSymbol)
    {
        var propertiesWithDataSources = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public &&
                       p.GetAttributes().Any(IsDataSourceAttribute))
            .ToList();

        if (propertiesWithDataSources.Count == 0)
        {
            return "new System.Collections.Generic.Dictionary<System.Reflection.PropertyInfo, global::TUnit.Core.IDataSourceProvider>()";
        }

        using var writer = new CodeWriter("", includeHeader: false);
        for (var i = 0; i < 2; i++) { writer._indentLevel++; } // Start with indent level 2 for inline dictionaries
        writer.AppendLine("new System.Collections.Generic.Dictionary<System.Reflection.PropertyInfo, global::TUnit.Core.IDataSourceProvider>");
        writer.AppendLine("{");

        foreach (var prop in propertiesWithDataSources)
        {
            var dataSourceAttr = prop.GetAttributes().FirstOrDefault(IsDataSourceAttribute);
            if (dataSourceAttr != null)
            {
                var providerCode = GenerateDataSourceProvider(dataSourceAttr, typeSymbol);
                if (!string.IsNullOrEmpty(providerCode))
                {
                    writer.AppendLine($"{{ typeof({typeSymbol.GloballyQualified()}).GetProperty(\"{prop.Name}\"), {providerCode} }},");
                }
            }
        }

        writer.AppendLine("}");
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
               (attr.AttributeClass!.AllInterfaces.Any(i =>
                   i.GloballyQualified() == "TUnit.Core.Interfaces.IDataAttribute"));
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
                // For custom IDataAttribute implementations
                return GenerateCustomDataProvider(attr);
        }
    }

    private static string GenerateInlineDataProvider(AttributeData attr)
    {
        var args = attr.ConstructorArguments.Select(TypedConstantParser.GetRawTypedConstantValue);
        return $"new global::TUnit.Core.DataSources.InlineDataSourceProvider(new object?[] {{ {string.Join(", ", args)} }})";
    }

    private static string GenerateMethodDataSourceProvider(AttributeData attr, INamedTypeSymbol containingType)
    {
        if (attr.ConstructorArguments.Length == 0)
        {
            return "";
        }

        var methodName = attr.ConstructorArguments[0].Value?.ToString() ?? "";
        var isShared = attr.NamedArguments.FirstOrDefault(na => na.Key == "Shared").Value.Value as bool? ?? false;

        return $"new global::TUnit.Core.DataSources.MethodDataSourceProvider(typeof({containingType.GloballyQualified()}).GetMethod(\"{methodName}\", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic), null, {isShared.ToString().ToLowerInvariant()})";
    }

    private static string GenerateClassDataSourceProvider(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length == 0)
        {
            return "";
        }

        var dataSourceType = attr.ConstructorArguments[0].Value as ITypeSymbol;
        if (dataSourceType == null)
        {
            return "";
        }

        var isShared = attr.NamedArguments.FirstOrDefault(na => na.Key == "Shared").Value.Value as bool? ?? false;

        return $"new global::TUnit.Core.DataSources.ClassDataSourceProvider(typeof({dataSourceType.GloballyQualified()}), {isShared.ToString().ToLowerInvariant()})";
    }

    private static string GeneratePropertyDataSourceProvider(AttributeData attr, INamedTypeSymbol containingType)
    {
        if (attr.ConstructorArguments.Length == 0)
        {
            return "";
        }

        var propertyName = attr.ConstructorArguments[0].Value?.ToString() ?? "";
        var isShared = attr.NamedArguments.FirstOrDefault(na => na.Key == "Shared").Value.Value as bool? ?? false;

        return $"new global::TUnit.Core.DataSources.PropertyDataSourceProvider(typeof({containingType.GloballyQualified()}), \"{propertyName}\", {isShared.ToString().ToLowerInvariant()})";
    }

    private static string GenerateCustomDataProvider(AttributeData attr)
    {
        // For custom data attributes, create an instance of the attribute and wrap it
        return $"new global::TUnit.Core.DataSources.AttributeDataSourceProvider({GenerateAttributeInstantiation(attr)})";
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
            .FirstOrDefault(attr => attr.AttributeClass!.GloballyQualified() == "TUnit.Core.RepeatAttribute");

        if (repeatAttr == null || repeatAttr.ConstructorArguments.Length == 0)
        {
            return 1;
        }

        if (repeatAttr.ConstructorArguments[0].Value is int count && count > 0)
        {
            return count;
        }

        return 1;
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
        var attributeStrings = new List<string>();

        foreach (var attr in allAttributes)
        {
            if (IsKnownTUnitAttribute(attr))
            {
                attributeStrings.Add(GenerateAttributeInstantiation(attr));
            }
            else
            {
                // For unknown attributes, use runtime helper
                attributeStrings.Add($"global::TUnit.Core.Helpers.RuntimeAttributeHelper.CreateAttribute(typeof({attr.AttributeClass!.GloballyQualified()}), {GenerateConstructorArgumentsArray(attr)}, {GenerateNamedArgumentsDictionary(attr)})");
            }
        }

        // Return as a single line to avoid parser issues with multi-line arrays
        return $"new System.Attribute[] {{ {string.Join(", ", attributeStrings)} }}";
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
            if (namedType.IsGenericType && !namedType.IsUnboundGenericType)
            {
                // This is a constructed generic type (e.g., List<int>, Dictionary<string, T>)
                var genericDef = GetGenericTypeDefinitionName(namedType);
                var genericArgs = namedType.TypeArguments.Select(GenerateTypeReference).ToArray();

                using var writer = new CodeWriter("", includeHeader: false);
        for (var i = 0; i < 1; i++) { writer._indentLevel++; }
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
        else if (typeParameter.DeclaringType != null)
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
        var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

        // For well-known types, use simplified names
        if (typeSymbol.ContainingAssembly.Name == "System.Private.CoreLib" ||
            typeSymbol.ContainingAssembly.Name == "mscorlib")
        {
            return $"{typeName}, System.Private.CoreLib";
        }

        return $"{typeName}, {typeSymbol.ContainingAssembly.Name}";
    }
}
