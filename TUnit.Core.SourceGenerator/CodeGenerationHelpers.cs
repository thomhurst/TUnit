using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace TUnit.Core.SourceGenerator;

/// <summary>
/// Helper methods for generating C# code strings from Roslyn symbols and metadata.
/// </summary>
internal static class CodeGenerationHelpers
{
    /// <summary>
    /// Generates a C# literal expression from a TypedConstant value.
    /// </summary>
    public static string GenerateCSharpLiteral(TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return "null";
        }

        switch (constant.Kind)
        {
            case TypedConstantKind.Primitive:
                return GeneratePrimitiveLiteral(constant.Value!, constant.Type!);

            case TypedConstantKind.Enum:
                return $"{GetFullTypeName(constant.Type!)}.{constant.Value}";

            case TypedConstantKind.Type:
                var typeSymbol = (ITypeSymbol)constant.Value!;
                return $"typeof({GetFullTypeName(typeSymbol)})";

            case TypedConstantKind.Array:
                var elementType = ((IArrayTypeSymbol)constant.Type!).ElementType;
                var elements = constant.Values.Select(GenerateCSharpLiteral);
                return $"new {GetFullTypeName(elementType)}[] {{ {string.Join(", ", elements)} }}";

            default:
                throw new NotSupportedException($"TypedConstant kind '{constant.Kind}' is not supported");
        }
    }

    /// <summary>
    /// Generates a primitive literal with proper formatting.
    /// </summary>
    private static string GeneratePrimitiveLiteral(object value, ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_String => $"@\"{((string)value).Replace("\"", "\"\"")}\"",
            SpecialType.System_Char => $"'{EscapeChar((char)value)}'",
            SpecialType.System_Boolean => value.ToString()!.ToLowerInvariant(),
            SpecialType.System_Single => $"{value}f",
            SpecialType.System_Double => $"{value}d",
            SpecialType.System_Decimal => $"{value}m",
            SpecialType.System_Int64 => $"{value}L",
            SpecialType.System_UInt32 => $"{value}u",
            SpecialType.System_UInt64 => $"{value}ul",
            SpecialType.System_Byte => $"(byte){value}",
            SpecialType.System_SByte => $"(sbyte){value}",
            SpecialType.System_Int16 => $"(short){value}",
            SpecialType.System_UInt16 => $"(ushort){value}",
            _ => value.ToString()!
        };
    }

    /// <summary>
    /// Escapes a character for use in a character literal.
    /// </summary>
    private static string EscapeChar(char c)
    {
        return c switch
        {
            '\'' => @"\'",
            '\\' => @"\\",
            '\0' => @"\0",
            '\a' => @"\a",
            '\b' => @"\b",
            '\f' => @"\f",
            '\n' => @"\n",
            '\r' => @"\r",
            '\t' => @"\t",
            '\v' => @"\v",
            _ => c.ToString()
        };
    }

    /// <summary>
    /// Generates C# code for a ParameterMetadata array from method parameters.
    /// </summary>
    public static string GenerateParameterMetadataArray(IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
        {
            return "System.Array.Empty<global::TUnit.Core.ParameterMetadata>()";
        }

        var sb = new StringBuilder();
        sb.AppendLine("new global::TUnit.Core.ParameterMetadata[]");
        sb.AppendLine("        {");

        foreach (var param in method.Parameters)
        {
            var parameterIndex = method.Parameters.IndexOf(param);
            sb.AppendLine($"            new global::TUnit.Core.ParameterMetadata(typeof({GetFullTypeName(param.Type)}))");
            sb.AppendLine("            {");
            sb.AppendLine($"                Name = \"{param.Name}\",");
            sb.AppendLine($"                Attributes = {GenerateAttributeMetadataArray(param.GetAttributes())},");
            sb.AppendLine($"                ReflectionInfo = typeof({GetFullTypeName(method.ContainingType)}).GetMethod(\"{method.Name}\", BindingFlags.Public | BindingFlags.Instance).GetParameters()[{parameterIndex}]");
            sb.AppendLine("            },");
        }

        sb.AppendLine("        }");
        return sb.ToString();
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

        var sb = new StringBuilder();
        sb.AppendLine("new global::TUnit.Core.AttributeMetadata[]");
        sb.AppendLine("        {");

        foreach (var attr in relevantAttributes)
        {
            var attributeCode = GenerateAttributeMetadata(attr);
            if (!string.IsNullOrEmpty(attributeCode))
            {
                sb.AppendLine(attributeCode);
            }
        }

        sb.AppendLine("        }");
        return sb.ToString();
    }

    /// <summary>
    /// Generates C# code for a single AttributeMetadata.
    /// </summary>
    private static string GenerateAttributeMetadata(AttributeData attr)
    {
        var sb = new StringBuilder();
        
        // For known TUnit attributes, generate direct instantiation
        if (IsKnownTUnitAttribute(attr))
        {
            sb.AppendLine("            new global::TUnit.Core.AttributeMetadata");
            sb.AppendLine("            {");
            sb.AppendLine($"                Instance = {GenerateAttributeInstantiation(attr)},");
            sb.AppendLine($"                TargetElement = global::TUnit.Core.TestAttributeTarget.Method,");
            sb.AppendLine("                ConstructorArguments = null,");
            sb.AppendLine("                NamedArguments = null");
            sb.AppendLine("            },");
        }
        else
        {
            // For unknown attributes, generate runtime construction info
            sb.AppendLine("            new global::TUnit.Core.AttributeMetadata");
            sb.AppendLine("            {");
            sb.AppendLine($"                Instance = global::TUnit.Core.Helpers.RuntimeAttributeHelper.CreateAttribute(");
            sb.AppendLine($"                    typeof({GetFullTypeName(attr.AttributeClass!)}),");
            sb.AppendLine($"                    {GenerateConstructorArgumentsArray(attr)},");
            sb.AppendLine($"                    {GenerateNamedArgumentsDictionary(attr)}),");
            sb.AppendLine($"                TargetElement = global::TUnit.Core.TestAttributeTarget.Method,");
            sb.AppendLine("                ConstructorArguments = null,");
            sb.AppendLine("                NamedArguments = null");
            sb.AppendLine("            },");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Determines if an attribute is a known TUnit attribute that we can safely instantiate.
    /// </summary>
    private static bool IsKnownTUnitAttribute(AttributeData attr)
    {
        var fullName = GetFullTypeName(attr.AttributeClass!);
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
        var typeName = GetFullTypeName(attr.AttributeClass!);
        var sb = new StringBuilder($"new {typeName}(");

        // Add constructor arguments
        if (attr.ConstructorArguments.Length > 0)
        {
            var args = attr.ConstructorArguments.Select(GenerateCSharpLiteral);
            sb.Append(string.Join(", ", args));
        }

        sb.Append(")");

        // Add named arguments
        if (attr.NamedArguments.Length > 0)
        {
            sb.Append(" { ");
            var namedArgs = attr.NamedArguments.Select(na => $"{na.Key} = {GenerateCSharpLiteral(na.Value)}");
            sb.Append(string.Join(", ", namedArgs));
            sb.Append(" }");
        }

        return sb.ToString();
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

        var args = attr.ConstructorArguments.Select(GenerateCSharpLiteral);
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

        var sb = new StringBuilder("new System.Collections.Generic.Dictionary<string, object?> { ");
        foreach (var na in attr.NamedArguments)
        {
            sb.Append($"{{ \"{na.Key}\", {GenerateCSharpLiteral(na.Value)} }}, ");
        }
        sb.Append("}");
        return sb.ToString();
    }

    /// <summary>
    /// Determines if an attribute should be excluded from metadata.
    /// </summary>
    private static bool IsCompilerGeneratedAttribute(AttributeData attr)
    {
        var fullName = GetFullTypeName(attr.AttributeClass!);
        return fullName.StartsWith("System.Runtime.CompilerServices.") ||
               fullName.StartsWith("System.Diagnostics.CodeAnalysis.");
    }

    /// <summary>
    /// Gets the fully qualified type name for a symbol.
    /// </summary>
    public static string GetFullTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
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

        var sb = new StringBuilder();
        sb.AppendLine("new global::TUnit.Core.PropertyMetadata[]");
        sb.AppendLine("        {");

        foreach (var prop in properties)
        {
            sb.AppendLine("            new global::TUnit.Core.PropertyMetadata");
            sb.AppendLine("            {");
            sb.AppendLine($"                Name = \"{prop.Name}\",");
            sb.AppendLine($"                Type = typeof({GetFullTypeName(prop.Type)}),");
            sb.AppendLine($"                ReflectionInfo = typeof({GetFullTypeName(typeSymbol)}).GetProperty(\"{prop.Name}\"),");
            sb.AppendLine($"                IsStatic = false,");
            sb.AppendLine($"                Getter = obj => ((({GetFullTypeName(typeSymbol)})obj).{prop.Name}),");
            sb.AppendLine($"                Attributes = {GenerateAttributeMetadataArray(prop.GetAttributes())}");
            sb.AppendLine("            },");
        }

        sb.AppendLine("        }");
        return sb.ToString();
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

        var sb = new StringBuilder();
        sb.AppendLine("new global::TUnit.Core.ConstructorMetadata[]");
        sb.AppendLine("        {");

        foreach (var ctor in constructors)
        {
            sb.AppendLine("            new global::TUnit.Core.ConstructorMetadata");
            sb.AppendLine("            {");
            sb.AppendLine($"                Type = typeof({GetFullTypeName(typeSymbol)}),");
            sb.AppendLine($"                Name = \".ctor\",");
            sb.AppendLine($"                Parameters = {GenerateParameterMetadataArray(ctor)},");
            sb.AppendLine($"                Attributes = {GenerateAttributeMetadataArray(ctor.GetAttributes())},");
            sb.AppendLine($"                IsStatic = false,");
            sb.AppendLine($"                IsPublic = {(ctor.DeclaredAccessibility == Accessibility.Public ? "true" : "false")},");
            sb.AppendLine($"                IsPrivate = {(ctor.DeclaredAccessibility == Accessibility.Private ? "true" : "false")},");
            sb.AppendLine($"                IsProtected = {(ctor.DeclaredAccessibility == Accessibility.Protected ? "true" : "false")},");
            sb.AppendLine($"                IsInternal = {(ctor.DeclaredAccessibility == Accessibility.Internal ? "true" : "false")}");
            sb.AppendLine("            },");
        }

        sb.AppendLine("        }");
        return sb.ToString();
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

        var sb = new StringBuilder();
        sb.AppendLine("new global::TUnit.Core.IDataSourceProvider[]");
        sb.AppendLine("        {");

        foreach (var attr in dataSourceAttributes)
        {
            var providerCode = GenerateDataSourceProvider(attr, typeSymbol);
            if (!string.IsNullOrEmpty(providerCode))
            {
                sb.AppendLine($"            {providerCode},");
            }
        }

        sb.AppendLine("        }");
        return sb.ToString();
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

        var sb = new StringBuilder();
        sb.AppendLine("new global::TUnit.Core.IDataSourceProvider[]");
        sb.AppendLine("        {");

        foreach (var attr in dataSourceAttributes)
        {
            var providerCode = GenerateDataSourceProvider(attr, methodSymbol.ContainingType);
            if (!string.IsNullOrEmpty(providerCode))
            {
                sb.AppendLine($"            {providerCode},");
            }
        }

        sb.AppendLine("        }");
        return sb.ToString();
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

        var sb = new StringBuilder();
        sb.AppendLine("new System.Collections.Generic.Dictionary<System.Reflection.PropertyInfo, global::TUnit.Core.IDataSourceProvider>");
        sb.AppendLine("        {");

        foreach (var prop in propertiesWithDataSources)
        {
            var dataSourceAttr = prop.GetAttributes().FirstOrDefault(IsDataSourceAttribute);
            if (dataSourceAttr != null)
            {
                var providerCode = GenerateDataSourceProvider(dataSourceAttr, typeSymbol);
                if (!string.IsNullOrEmpty(providerCode))
                {
                    sb.AppendLine($"            {{ typeof({GetFullTypeName(typeSymbol)}).GetProperty(\"{prop.Name}\"), {providerCode} }},");
                }
            }
        }

        sb.AppendLine("        }");
        return sb.ToString();
    }

    /// <summary>
    /// Determines if an attribute is a data source attribute.
    /// </summary>
    private static bool IsDataSourceAttribute(AttributeData attr)
    {
        var fullName = GetFullTypeName(attr.AttributeClass!);
        return fullName == "TUnit.Core.ArgumentsAttribute" ||
               fullName == "TUnit.Core.InlineDataAttribute" ||
               fullName == "TUnit.Core.MethodDataSourceAttribute" ||
               fullName == "TUnit.Core.ClassDataSourceAttribute" ||
               fullName == "TUnit.Core.PropertyDataSourceAttribute" ||
               (attr.AttributeClass!.AllInterfaces.Any(i => 
                   GetFullTypeName(i) == "TUnit.Core.Interfaces.IDataAttribute"));
    }

    /// <summary>
    /// Generates a data source provider instance based on the attribute type.
    /// </summary>
    private static string GenerateDataSourceProvider(AttributeData attr, INamedTypeSymbol containingType)
    {
        var fullName = GetFullTypeName(attr.AttributeClass!);

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
        var args = attr.ConstructorArguments.Select(GenerateCSharpLiteral);
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

        return $"new global::TUnit.Core.DataSources.MethodDataSourceProvider(typeof({GetFullTypeName(containingType)}).GetMethod(\"{methodName}\", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic), null, {isShared.ToString().ToLowerInvariant()})";
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

        return $"new global::TUnit.Core.DataSources.ClassDataSourceProvider(typeof({GetFullTypeName(dataSourceType)}), {isShared.ToString().ToLowerInvariant()})";
    }

    private static string GeneratePropertyDataSourceProvider(AttributeData attr, INamedTypeSymbol containingType)
    {
        if (attr.ConstructorArguments.Length == 0)
        {
            return "";
        }

        var propertyName = attr.ConstructorArguments[0].Value?.ToString() ?? "";
        var isShared = attr.NamedArguments.FirstOrDefault(na => na.Key == "Shared").Value.Value as bool? ?? false;

        return $"new global::TUnit.Core.DataSources.PropertyDataSourceProvider(typeof({GetFullTypeName(containingType)}), \"{propertyName}\", {isShared.ToString().ToLowerInvariant()})";
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
            .FirstOrDefault(attr => GetFullTypeName(attr.AttributeClass!) == "TUnit.Core.TimeoutAttribute") ??
            methodSymbol.ContainingType.GetAttributes()
            .FirstOrDefault(attr => GetFullTypeName(attr.AttributeClass!) == "TUnit.Core.TimeoutAttribute");

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
            .FirstOrDefault(attr => GetFullTypeName(attr.AttributeClass!) == "TUnit.Core.SkipAttribute") ??
            methodSymbol.ContainingType.GetAttributes()
            .FirstOrDefault(attr => GetFullTypeName(attr.AttributeClass!) == "TUnit.Core.SkipAttribute");

        if (skipAttr == null)
        {
            return (false, "null");
        }

        var reason = skipAttr.ConstructorArguments.Length > 0 
            ? GenerateCSharpLiteral(skipAttr.ConstructorArguments[0])
            : "\"Test skipped\"";

        return (true, reason);
    }

    /// <summary>
    /// Extracts repeat count from repeat attributes.
    /// </summary>
    public static int ExtractRepeatCount(IMethodSymbol methodSymbol)
    {
        var repeatAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(attr => GetFullTypeName(attr.AttributeClass!) == "TUnit.Core.RepeatAttribute");

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

        var sb = new StringBuilder();
        sb.AppendLine("new System.Attribute[]");
        sb.AppendLine("        {");

        foreach (var attr in allAttributes)
        {
            if (IsKnownTUnitAttribute(attr))
            {
                sb.AppendLine($"            {GenerateAttributeInstantiation(attr)},");
            }
            else
            {
                // For unknown attributes, use runtime helper
                sb.AppendLine($"            global::TUnit.Core.Helpers.RuntimeAttributeHelper.CreateAttribute(");
                sb.AppendLine($"                typeof({GetFullTypeName(attr.AttributeClass!)}),");
                sb.AppendLine($"                {GenerateConstructorArgumentsArray(attr)},");
                sb.AppendLine($"                {GenerateNamedArgumentsDictionary(attr)}),");
            }
        }

        sb.AppendLine("        }");
        return sb.ToString();
    }
}