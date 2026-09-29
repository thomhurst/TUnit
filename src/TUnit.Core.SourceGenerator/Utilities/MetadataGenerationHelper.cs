using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Utilities;

/// <summary>
/// Centralized helper for generating metadata object instantiation code
/// </summary>
internal static class MetadataGenerationHelper
{
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
        writer.AppendLine($"TypeInfo = {CodeGenerationHelpers.GenerateTypeInfo(methodSymbol.ContainingType)},");
        writer.AppendLine($"Name = \"{methodSymbol.Name}\",");
        writer.AppendLine($"GenericTypeCount = {methodSymbol.TypeParameters.Length},");
        writer.AppendLine($"ReturnType = typeof({safeReturnTypeDisplay}),");
        writer.AppendLine($"ReturnTypeInfo = {CodeGenerationHelpers.GenerateTypeInfo(methodSymbol.ReturnType)},");
        writer.Append($"Parameters = ");
        WriteParameterMetadataArrayForMethod(writer, methodSymbol);
        writer.AppendLine(",");
        writer.Append("Class = ");
        WriteClassMetadataGetOrAddWithParent(writer, namedTypeSymbol);

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
        writer.AppendLine($"TypeInfo = {CodeGenerationHelpers.GenerateTypeInfo(methodSymbol.ContainingType)},");
        writer.AppendLine($"Name = \"{methodSymbol.Name}\",");
        writer.AppendLine($"GenericTypeCount = {methodSymbol.TypeParameters.Length},");
        writer.AppendLine($"ReturnType = typeof({safeReturnTypeDisplay}),");
        writer.AppendLine($"ReturnTypeInfo = {CodeGenerationHelpers.GenerateTypeInfo(methodSymbol.ReturnType)},");
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
        writer.AppendLine($"global::TUnit.Core.ClassMetadata.GetOrAdd(\"{qualifiedName}\", new global::TUnit.Core.ClassMetadata");
        writer.AppendLine("{");

        // Manually increment indent level without calling EnsureNewLine
        var currentIndent = writer.IndentLevel;
        writer.SetIndentLevel(currentIndent + 1);

        writer.AppendLine($"Type = typeof({typeSymbol.GloballyQualified()}),");
        writer.AppendLine($"TypeInfo = {CodeGenerationHelpers.GenerateTypeInfo(typeSymbol)},");
        writer.AppendLine($"Name = \"{typeSymbol.Name}\",");
        writer.AppendLine($"Namespace = \"{typeSymbol.ContainingNamespace?.ToDisplayString() ?? ""}\",");
        writer.AppendLine($"Assembly = {GenerateAssemblyMetadataGetOrAdd(typeSymbol.ContainingAssembly)},");

        // For abstract classes, skip constructor processing since they cannot be instantiated directly
        // For concrete classes, only consider public constructors
        var constructor = typeSymbol.IsAbstract
            ? null
            : typeSymbol.InstanceConstructors.FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public);
        var constructorParams = constructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        if (constructor != null && constructorParams.Length > 0)
        {
            writer.Append("Parameters = ");
            WriteParameterMetadataArrayForConstructor(writer, constructor);
            writer.AppendLine(",");
        }
        else
        {
            writer.AppendLine("Parameters = global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>(),");
        }

        writer.Append("Properties = ");
        WritePropertyMetadataArray(writer, typeSymbol, out _);
        writer.AppendLine(",");
        writer.Append($"Parent = {parentExpression ?? "null"}");

        // Back to original level
        writer.SetIndentLevel(currentIndent);
        writer.AppendLine();
        writer.Append("})");
    }

    /// <summary>
    /// Generates ClassMetadata with recursive parent as a string expression.
    /// Used by the per-class path to pre-generate shared locals.
    /// </summary>
    public static string GenerateClassMetadataGetOrAddWithParentExpression(INamedTypeSymbol typeSymbol, int indentLevel = 0)
    {
        var writer = new CodeWriter(includeHeader: false).SetIndentLevel(indentLevel);
        WriteClassMetadataGetOrAddWithParent(writer, typeSymbol);
        return writer.ToString();
    }

    /// <summary>
    /// Generates a ParameterMetadata[] expression for a method's parameters as a string.
    /// Returns null if the method has no parameters.
    /// </summary>
    public static string? GenerateParameterMetadataArrayForMethodExpression(IMethodSymbol method, int indentLevel = 0)
    {
        if (method.Parameters.Length == 0)
        {
            return null;
        }

        var writer = new CodeWriter("", includeHeader: false).SetIndentLevel(indentLevel);
        WriteParameterMetadataArrayForMethod(writer, method);
        return writer.ToString();
    }

    /// <summary>
    /// Writes ClassMetadata with recursive parent generation for nested types
    /// </summary>
    private static void WriteClassMetadataGetOrAddWithParent(ICodeWriter writer, INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.ContainingType != null)
        {
            // Generate the parent expression as a string, then pass it
            var parentWriter = new CodeWriter("", includeHeader: false).SetIndentLevel(writer.IndentLevel);
            WriteClassMetadataGetOrAddWithParent(parentWriter, typeSymbol.ContainingType);
            WriteClassMetadataGetOrAdd(writer, typeSymbol, parentWriter.ToString());
        }
        else
        {
            WriteClassMetadataGetOrAdd(writer, typeSymbol);
        }
    }

    /// <summary>
    /// Generates code for creating a ClassMetadata instance with GetOrAdd pattern
    /// </summary>
    public static string GenerateClassMetadataGetOrAdd(INamedTypeSymbol typeSymbol, string? parentExpression = null, int currentIndentLevel = 0)
    {
        var qualifiedName = $"{typeSymbol.ContainingAssembly.Name}:{typeSymbol.GloballyQualified()}";
        var writer = new CodeWriter("", includeHeader: false).SetIndentLevel(currentIndentLevel);
        writer.AppendLine($"global::TUnit.Core.ClassMetadata.GetOrAdd(\"{qualifiedName}\", new global::TUnit.Core.ClassMetadata");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"Type = typeof({typeSymbol.GloballyQualified()}),");
        writer.AppendLine($"TypeInfo = {CodeGenerationHelpers.GenerateTypeInfo(typeSymbol)},");
        writer.AppendLine($"Name = \"{typeSymbol.Name}\",");
        writer.AppendLine($"Namespace = \"{typeSymbol.ContainingNamespace?.ToDisplayString() ?? ""}\",");
        writer.AppendLine($"Assembly = {GenerateAssemblyMetadataGetOrAdd(typeSymbol.ContainingAssembly)},");
        // For abstract classes, skip constructor processing since they cannot be instantiated directly
        // For concrete classes, only consider public constructors
        var constructor = typeSymbol.IsAbstract
            ? null
            : typeSymbol.InstanceConstructors.FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public);
        var constructorParams = constructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        if (constructor != null && constructorParams.Length > 0)
        {
            writer.AppendLine($"Parameters = {GenerateParameterMetadataArrayForConstructor(constructor, writer.IndentLevel)},");
        }
        else
        {
            writer.AppendLine("Parameters = global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>(),");
        }
        writer.AppendLine($"Properties = {GeneratePropertyMetadataArray(typeSymbol, writer.IndentLevel, out _)},");
        writer.AppendLine($"Parent = {parentExpression ?? "null"}");

        writer.Unindent();
        writer.Append("})");

        return writer.ToString();
    }

    /// <summary>
    /// Generates code for creating an AssemblyMetadata instance with GetOrAdd pattern
    /// </summary>
    public static string GenerateAssemblyMetadataGetOrAdd(IAssemblySymbol assembly)
    {
        return $"global::TUnit.Core.AssemblyMetadata.GetOrAdd(\"{assembly.Name}\", \"{assembly.Name}\")";
    }

    /// <summary>
    /// Generates code for creating a ParameterMetadata instance via ParameterMetadataFactory.Create().
    /// Reflection info is attached per method/constructor by <see cref="WriteReflectionInfoAttachStart"/>.
    /// </summary>
    private static void WriteParameterMetadata(ICodeWriter writer, IParameterSymbol parameter)
    {
        var safeType = CodeGenerationHelpers.ContainsTypeParameter(parameter.Type) ? "object" : parameter.Type.GloballyQualified();

        writer.Append($"global::TUnit.Core.ParameterMetadataFactory.Create(typeof({safeType}), \"{parameter.Name}\", {CodeGenerationHelpers.GenerateTypeInfo(parameter.Type)}, {parameter.Type.IsNullable().ToString().ToLowerInvariant()})");
    }

    /// <summary>
    /// Writes the opening of a ParameterMetadataFactory.ForMethod/ForGenericMethod/ForConstructor call, which lazily
    /// resolves the ParameterInfo of every parameter from a single shared method lookup on first access.
    /// Must be followed by the ParameterMetadata[] array expression and a closing parenthesis.
    /// </summary>
    private static void WriteReflectionInfoAttachStart(ICodeWriter writer, IMethodSymbol method)
    {
        var containingType = method.ContainingType.GloballyQualified();
        var usesTypeParameters = method.Parameters.Any(p => CodeGenerationHelpers.ContainsTypeParameter(p.Type));

        if (method.MethodKind == MethodKind.Constructor)
        {
            writer.Append($"global::TUnit.Core.ParameterMetadataFactory.ForConstructor(typeof({containingType}), {usesTypeParameters.ToString().ToLowerInvariant()}, ");
        }
        else if (method.DeclaredAccessibility != Accessibility.Public)
        {
            // The factory's declaring-type annotation only keeps public methods, so trimming stays limited to what
            // ClassMetadata.Type already keeps. A non-public method is kept instead by a no-op delegate carrying
            // [DynamicDependency] with its exact signature. Annotating the type with NonPublicMethods would keep
            // every private helper, and a GetMethod(name, ...) intrinsic would keep every same-name overload; both
            // report IL2111 for helpers with [DynamicallyAccessedMembers] parameters. The attribute is emitted for every
            // target: a .NET Standard test library can end up in a trimmed app, and DynamicDependencyPolyfillGenerator
            // declares the attribute where the framework lacks it.
            writer.Append($"global::TUnit.Core.ParameterMetadataFactory.ForNonPublicMethod(typeof({containingType}), \"{method.Name}\", {method.IsStatic.ToString().ToLowerInvariant()}, {method.TypeParameters.Length}, [global::System.Diagnostics.CodeAnalysis.DynamicDependency(\"{GetDynamicDependencySignature(method)}\", typeof({containingType}))] static () => {{ }}, ");
        }
        else if (method.TypeParameters.Length > 0 || usesTypeParameters)
        {
            writer.Append($"global::TUnit.Core.ParameterMetadataFactory.ForGenericMethod(typeof({containingType}), \"{method.Name}\", {method.IsStatic.ToString().ToLowerInvariant()}, {method.TypeParameters.Length}, ");
        }
        else
        {
            writer.Append($"global::TUnit.Core.ParameterMetadataFactory.ForMethod(typeof({containingType}), \"{method.Name}\", {method.IsStatic.ToString().ToLowerInvariant()}, ");
        }
    }

    /// <summary>
    /// Returns the method's signature in the documentation-comment ID form that
    /// <see cref="System.Diagnostics.CodeAnalysis.DynamicDependencyAttribute"/> expects, e.g.
    /// <c>Run``1(``0,System.Int32)</c>: the member's doc ID without its "M:" prefix and declaring type.
    /// </summary>
    private static string GetDynamicDependencySignature(IMethodSymbol method)
    {
        var methodId = method.OriginalDefinition.GetDocumentationCommentId();
        var typeId = method.ContainingType.OriginalDefinition.GetDocumentationCommentId();

        if (methodId is null || typeId is null || !methodId.StartsWith("M:" + typeId.Substring(2) + ".", StringComparison.Ordinal))
        {
            return method.Name;
        }

        return methodId.Substring(typeId.Length + 1);
    }

    /// <summary>
    /// Generates code for creating a PropertyMetadata instance
    /// </summary>
    public static void WritePropertyMetadata(ICodeWriter writer, IPropertySymbol property, INamedTypeSymbol containingType)
    {
        var safeTypeNameForReflection = containingType.GloballyQualified();
        // For type parameters, we need to use typeof(object) instead of typeof(T)
        var containsTypeParameter = CodeGenerationHelpers.ContainsTypeParameter(property.Type);
        var safePropertyTypeName = containsTypeParameter ? "object" : property.Type.GloballyQualified();

        writer.AppendLine("new global::TUnit.Core.PropertyMetadata");
        writer.AppendLine("{");

        // Manually increment indent level without calling EnsureNewLine
        var currentIndent = writer.IndentLevel;
        writer.SetIndentLevel(currentIndent + 1);

        var reflectionInfo = containsTypeParameter
            ? $"typeof({safeTypeNameForReflection}).GetProperty(\"{property.Name}\")"
            : $"typeof({safeTypeNameForReflection}).GetProperty(\"{property.Name}\", typeof({safePropertyTypeName}))";

        writer.AppendLine($"ReflectionInfo = {reflectionInfo},");
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
                                         (namedTypeSymbol.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter) ||
                                          namedTypeSymbol.TypeArguments.OfType<ITypeParameterSymbol>().Any() ||
                                          SymbolEqualityComparer.Default.Equals(namedTypeSymbol, namedTypeSymbol.OriginalDefinition));

        if (hasUnresolvedTypeParameters)
        {
            return property.IsStatic
                // Can't access static members on an unbound generic type like WebApplicationTest<,>
                // Use reflection to get the value at runtime
                ? $"_ => typeof({namedTypeSymbol.GloballyQualified()}).GetProperty(\"{property.Name}\")?.GetValue(null)"
                // Use dynamic to avoid invalid cast to open generic type
                : $"o => ((dynamic)o).{property.Name}";
        }

        var safeTypeName = namedTypeSymbol.GloballyQualified();
        return property.IsStatic
            ? $"_ => {safeTypeName}.{property.Name}"
            : $"o => (({safeTypeName})o).{property.Name}";
    }

    /// <summary>
    /// Writes an array of ParameterMetadata objects for method parameters with proper reflection info.
    /// </summary>
    private static void WriteParameterMetadataArrayForMethod(ICodeWriter writer, IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
        {
            writer.Append("global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>()");
            return;
        }

        WriteReflectionInfoAttachStart(writer, method);
        writer.AppendLine("new global::TUnit.Core.ParameterMetadata[]");
        writer.AppendLine("{");

        // Manually increment indent level without calling EnsureNewLine
        var currentIndent = writer.IndentLevel;
        writer.SetIndentLevel(currentIndent + 1);

        for (var i = 0; i < method.Parameters.Length; i++)
        {
            var param = method.Parameters[i];
            WriteParameterMetadata(writer, param);

            if (i < method.Parameters.Length - 1)
            {
                writer.AppendLine(",");
            }
        }

        // Manually restore indent level
        writer.SetIndentLevel(currentIndent);
        writer.AppendLine();
        writer.Append("})");
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
    private static void WriteParameterMetadataArrayForConstructor(ICodeWriter writer, IMethodSymbol constructor)
    {
        if (constructor.Parameters.Length == 0)
        {
            writer.Append("global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>()");
            return;
        }

        WriteReflectionInfoAttachStart(writer, constructor);
        writer.AppendLine("new global::TUnit.Core.ParameterMetadata[]");
        writer.AppendLine("{");

        // Manually increment indent level without calling EnsureNewLine
        var currentIndent = writer.IndentLevel;
        writer.SetIndentLevel(currentIndent + 1);

        for (var i = 0; i < constructor.Parameters.Length; i++)
        {
            var param = constructor.Parameters[i];
            WriteParameterMetadata(writer, param);

            if (i < constructor.Parameters.Length - 1)
            {
                writer.AppendLine(",");
            }
        }

        // Manually restore indent level
        writer.SetIndentLevel(currentIndent);
        writer.AppendLine();
        writer.Append("})");
    }

    /// <summary>
    /// Generates an array of ParameterMetadata objects for constructor parameters with proper reflection info
    /// </summary>
    private static string GenerateParameterMetadataArrayForConstructor(IMethodSymbol constructor, int currentIndentLevel = 0)
    {
        if (constructor.Parameters.Length == 0)
        {
            return "global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>()";
        }

        var writer = new CodeWriter("", includeHeader: false).SetIndentLevel(currentIndentLevel);
        WriteReflectionInfoAttachStart(writer, constructor);
        writer.AppendLine("new global::TUnit.Core.ParameterMetadata[]");
        writer.AppendLine("{");
        writer.Indent();

        for (var i = 0; i < constructor.Parameters.Length; i++)
        {
            var param = constructor.Parameters[i];
            WriteParameterMetadata(writer, param);

            if (i < constructor.Parameters.Length - 1)
            {
                writer.AppendLine(",");
            }
        }

        writer.Unindent();
        writer.Append("})");

        return writer.ToString();
    }

    /// <summary>
    /// Writes an array of PropertyMetadata objects
    /// </summary>
    private static void WritePropertyMetadataArray(ICodeWriter writer, INamedTypeSymbol typeSymbol, out int propertyCount)
    {
        var properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.Name != "EqualityContract")
            .ToList();

        propertyCount = properties.Count;

        if (properties.Count == 0)
        {
            writer.Append("global::System.Array.Empty<global::TUnit.Core.PropertyMetadata>()");
            return;
        }

        writer.AppendLine("new global::TUnit.Core.PropertyMetadata[]");
        writer.AppendLine("{");

        // Manually increment indent level without calling EnsureNewLine
        var currentIndent = writer.IndentLevel;
        writer.SetIndentLevel(currentIndent + 1);

        for (var i = 0; i < properties.Count; i++)
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
    private static string GeneratePropertyMetadataArray(INamedTypeSymbol typeSymbol, int currentIndentLevel, out int propertyCount)
    {
        var properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.Name != "EqualityContract")
            .ToList();

        propertyCount = properties.Count;

        if (properties.Count == 0)
        {
            return "global::System.Array.Empty<global::TUnit.Core.PropertyMetadata>()";
        }

        var writer = new CodeWriter("", includeHeader: false).SetIndentLevel(currentIndentLevel);
        writer.AppendLine("new global::TUnit.Core.PropertyMetadata[]");
        writer.AppendLine("{");
        writer.Indent();

        for (var i = 0; i < properties.Count; i++)
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
