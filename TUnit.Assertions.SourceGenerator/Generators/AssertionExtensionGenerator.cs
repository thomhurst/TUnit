using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Assertions.SourceGenerator.Generators;

/// <summary>
/// Source generator that creates extension methods for assertion classes decorated with [AssertionExtension].
/// Analyzes the constructors of assertion classes and generates corresponding extension methods that:
/// - Extend IAssertionSource&lt;T&gt;
/// - Build the expression string for error messages
/// - Instantiate the assertion with the provided parameters
/// </summary>
[Generator]
public sealed class AssertionExtensionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes decorated with [AssertionExtension]
        var assertionClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Assertions.Attributes.AssertionExtensionAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => GetAssertionExtensionData(ctx, ct))
            .Where(static x => x != null)
            .Select(static (x, _) => x!);

        // Generate extension methods for each assertion class
        context.RegisterSourceOutput(assertionClasses, static (context, data) => GenerateExtensionMethods(context, data));
    }

    private static AssertionExtensionData? GetAssertionExtensionData(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        // Get the [AssertionExtension] attribute
        var attributeData = context.Attributes.FirstOrDefault();
        if (attributeData == null)
        {
            return null;
        }

        // Extract method name from attribute
        string? methodName = null;
        if (attributeData.ConstructorArguments.Length > 0)
        {
            methodName = attributeData.ConstructorArguments[0].Value?.ToString();
        }

        if (string.IsNullOrEmpty(methodName))
        {
            return null;
        }

        // Extract optional negated method name and overload resolution priority
        string? negatedMethodName = null;
        int overloadPriority = 0;  // Default to 0
        foreach (var namedArg in attributeData.NamedArguments)
        {
            if (namedArg.Key == "NegatedMethodName")
            {
                negatedMethodName = namedArg.Value.Value?.ToString();
            }
            else if (namedArg.Key == "OverloadResolutionPriority" && namedArg.Value.Value is int priority)
            {
                overloadPriority = priority;
            }
        }

        // Find the base Assertion<T> type to extract the type parameter
        var assertionBaseType = GetAssertionBaseType(classSymbol);
        if (assertionBaseType == null)
        {
            // Not an Assertion<T> - skip
            return null;
        }

        // Get all public constructors
        var constructors = classSymbol.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .ToImmutableArray();

        if (constructors.Length == 0)
        {
            return null;
        }

        // Check for RequiresUnreferencedCode attribute
        var RequiresUnreferencedCodeAttr = classSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "RequiresUnreferencedCodeAttribute");
        string? RequiresUnreferencedCodeMessage = null;
        if (RequiresUnreferencedCodeAttr != null && RequiresUnreferencedCodeAttr.ConstructorArguments.Length > 0)
        {
            RequiresUnreferencedCodeMessage = RequiresUnreferencedCodeAttr.ConstructorArguments[0].Value?.ToString();
        }

        return new AssertionExtensionData(
            classSymbol,
            methodName!,
            negatedMethodName,
            assertionBaseType,
            constructors,
            overloadPriority,
            RequiresUnreferencedCodeMessage
        );
    }

    private static INamedTypeSymbol? GetAssertionBaseType(INamedTypeSymbol classSymbol)
    {
        var currentType = classSymbol.BaseType;
        while (currentType != null)
        {
            if (currentType.Name == "Assertion" &&
                currentType.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions.Core" &&
                currentType.TypeArguments.Length == 1)
            {
                return currentType;
            }
            currentType = currentType.BaseType;
        }
        return null;
    }

    private static void GenerateExtensionMethods(SourceProductionContext context, AssertionExtensionData data)
    {
        var sourceBuilder = new StringBuilder();

        // Header
        sourceBuilder.AppendLine("#nullable enable");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
        sourceBuilder.AppendLine("using TUnit.Assertions.Core;");
        sourceBuilder.AppendLine("using TUnit.Assertions.Enums;");

        // Add using for the assertion class's namespace if different
        var assertionNamespace = data.ClassSymbol.ContainingNamespace?.ToDisplayString();
        if (!string.IsNullOrEmpty(assertionNamespace) && assertionNamespace != "TUnit.Assertions.Extensions")
        {
            sourceBuilder.AppendLine($"using {assertionNamespace};");
        }

        sourceBuilder.AppendLine();

        // Namespace

        sourceBuilder.AppendLine($"namespace TUnit.Assertions.Extensions;");
        sourceBuilder.AppendLine();

        // Extension class
        var extensionClassName = $"{data.ClassSymbol.Name}Extensions";
        sourceBuilder.AppendLine($"/// <summary>");
        sourceBuilder.AppendLine($"/// Generated extension methods for {data.ClassSymbol.Name}.");
        sourceBuilder.AppendLine($"/// </summary>");
        sourceBuilder.AppendLine($"public static class {extensionClassName}");
        sourceBuilder.AppendLine("{");

        // Generate extension methods for each constructor
        foreach (var constructor in data.Constructors)
        {
            if (!IsValidConstructor(constructor))
            {
                continue;
            }

            // Check if the type parameter is a nullable reference type (e.g., string?)
            var typeParam = data.AssertionBaseType.TypeArguments[0];
            var isNullableReferenceType = typeParam.NullableAnnotation == NullableAnnotation.Annotated &&
                                         typeParam.IsReferenceType;

            // Generate positive assertion method
            GenerateExtensionMethod(sourceBuilder, data, constructor, negated: false, isNullableOverload: false);

            // Generate negated assertion method if requested
            if (!string.IsNullOrEmpty(data.NegatedMethodName))
            {
                GenerateExtensionMethod(sourceBuilder, data, constructor, negated: true, isNullableOverload: false);
            }
        }

        sourceBuilder.AppendLine("}");

        // Add source to compilation
        var fileName = $"{data.ClassSymbol.Name}.Extensions.g.cs";
        context.AddSource(fileName, sourceBuilder.ToString());
    }

    private static bool IsValidConstructor(IMethodSymbol constructor)
    {
        // Must have at least one parameter
        if (constructor.Parameters.Length == 0)
        {
            return false;
        }

        // First parameter must be AssertionContext<T>
        var firstParam = constructor.Parameters[0];
        if (firstParam.Type is not INamedTypeSymbol firstParamType)
        {
            return false;
        }

        return firstParamType.Name == "AssertionContext" &&
               firstParamType.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions.Core" &&
               firstParamType.TypeArguments.Length == 1;
    }

    private static void GenerateExtensionMethod(
        StringBuilder sourceBuilder,
        AssertionExtensionData data,
        IMethodSymbol constructor,
        bool negated,
        bool isNullableOverload)
    {
        var methodName = negated ? data.NegatedMethodName : data.MethodName;
        var assertionType = data.ClassSymbol;
        var typeParam = data.AssertionBaseType.TypeArguments[0];

        // Skip the first parameter (AssertionContext<T>)
        var additionalParams = constructor.Parameters.Skip(1).ToArray();

        // Check for RequiresUnreferencedCode attribute on the constructor first, then fall back to class-level
        var constructorRequiresUnreferencedCodeAttr = constructor.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "RequiresUnreferencedCodeAttribute");

        string? requiresUnreferencedCodeMessage = null;
        if (constructorRequiresUnreferencedCodeAttr != null && constructorRequiresUnreferencedCodeAttr.ConstructorArguments.Length > 0)
        {
            // Constructor-level attribute takes precedence
            requiresUnreferencedCodeMessage = constructorRequiresUnreferencedCodeAttr.ConstructorArguments[0].Value?.ToString();
        }
        else if (!string.IsNullOrEmpty(data.RequiresUnreferencedCodeMessage))
        {
            // Fall back to class-level attribute
            requiresUnreferencedCodeMessage = data.RequiresUnreferencedCodeMessage;
        }

        // Build generic type parameters string
        // Use the assertion class's own type parameters if it has them
        var genericParams = new List<string>();
        var typeConstraints = new List<string>();

        // NEW: Detect if this is a multi-parameter generic assertion (e.g., collection assertions)
        // Check if the assertion class has multiple type parameters beyond just Assertion<T>
        var isMultiParameterGeneric = assertionType.TypeParameters.Length > 1;

        if (assertionType.IsGenericType && assertionType.TypeParameters.Length > 0)
        {
            // The assertion class defines its own generic type parameters
            // e.g., GreaterThanAssertion<TValue> or CollectionContainsAssertion<TCollection, TItem>
            foreach (var typeParameter in assertionType.TypeParameters)
            {
                genericParams.Add(typeParameter.Name);

                // Collect constraints for each type parameter
                var constraints = new List<string>();
                if (typeParameter.HasReferenceTypeConstraint)
                {
                    constraints.Add("class");
                }
                if (typeParameter.HasValueTypeConstraint)
                {
                    constraints.Add("struct");
                }
                if (typeParameter.HasNotNullConstraint)
                {
                    constraints.Add("notnull");
                }
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    constraints.Add(constraintType.ToDisplayString());
                }
                if (typeParameter.HasConstructorConstraint)
                {
                    constraints.Add("new()");
                }

                if (constraints.Count > 0)
                {
                    typeConstraints.Add($"where {typeParameter.Name} : {string.Join(", ", constraints)}");
                }
            }
        }
        else if (typeParam is ITypeParameterSymbol typeParamSymbol)
        {
            // The assertion class is not generic, but inherits from Assertion<T>
            // where T is a type parameter from the base class
            genericParams.Add(typeParamSymbol.Name);

            // Collect constraints
            var constraints = new List<string>();
            if (typeParamSymbol.HasReferenceTypeConstraint)
            {
                constraints.Add("class");
            }
            if (typeParamSymbol.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }
            if (typeParamSymbol.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }
            foreach (var constraintType in typeParamSymbol.ConstraintTypes)
            {
                constraints.Add(constraintType.ToDisplayString());
            }
            if (typeParamSymbol.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            if (constraints.Count > 0)
            {
                typeConstraints.Add($"where {typeParamSymbol.Name} : {string.Join(", ", constraints)}");
            }
        }

        var genericParamsString = genericParams.Count > 0 ? $"<{string.Join(", ", genericParams)}>" : "";

        // Build method signature
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    /// <summary>");
        sourceBuilder.AppendLine($"    /// Extension method for {assertionType.Name}.");
        sourceBuilder.AppendLine("    /// </summary>");

        // Add RequiresUnreferencedCode attribute if present (from constructor or class level)
        if (!string.IsNullOrEmpty(requiresUnreferencedCodeMessage))
        {
            var escapedMessage = requiresUnreferencedCodeMessage!.Replace("\"", "\\\"");
            sourceBuilder.AppendLine($"    [global::System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(\"{escapedMessage}\")]");
        }

        // Add OverloadResolutionPriority attribute if specified
        // For nullable overloads (generic with class constraint), increase priority by 1
        // so they're preferred over the base nullable overload when source is non-nullable
        var effectivePriority = data.OverloadResolutionPriority;
        if (isNullableOverload)
        {
            effectivePriority += 1;
        }

        if (effectivePriority > 0)
        {
            sourceBuilder.AppendLine($"    [global::System.Runtime.CompilerServices.OverloadResolutionPriority({effectivePriority})]");
        }

        // Method declaration
        var returnType = assertionType.IsGenericType
            ? $"{assertionType.Name}{genericParamsString}"
            : assertionType.Name;

        // The extension method extends IAssertionSource<T> where T is the type argument
        // from the Assertion<T> base class.
        string sourceType;
        string genericTypeParam = null;
        string genericConstraint = null;

        if (isNullableOverload)
        {
            // For nullable reference types, we can't use two separate overloads for T and T?
            // because NRT annotations are erased at runtime - they're the same type to the CLR.
            // Instead, just use the nullable version and accept both nullable and non-nullable sources.
            sourceType = $"IAssertionSource<{typeParam.ToDisplayString()}>";
            genericTypeParam = null;
            genericConstraint = null;
        }
        else if (typeParam is ITypeParameterSymbol baseTypeParam)
        {
            sourceType = $"IAssertionSource<{baseTypeParam.Name}>";
        }
        else
        {
            sourceType = $"IAssertionSource<{typeParam.ToDisplayString()}>";
        }

        sourceBuilder.Append($"    public static {returnType} {methodName}");
        if (genericTypeParam != null)
        {
            sourceBuilder.Append($"<{genericTypeParam}>");
        }
        sourceBuilder.Append(genericParamsString);
        sourceBuilder.Append("(");
        sourceBuilder.Append($"this {sourceType} source");

        // Add additional parameters
        foreach (var param in additionalParams)
        {
            sourceBuilder.Append($", {param.Type.ToDisplayString()} {param.Name}");

            // Add default value if present
            if (param.HasExplicitDefaultValue)
            {
                var defaultValue = FormatDefaultValue(param.ExplicitDefaultValue, param.Type);
                sourceBuilder.Append($" = {defaultValue}");
            }
        }

        // Add CallerArgumentExpression parameters for better error messages
        for (int i = 0; i < additionalParams.Length; i++)
        {
            var param = additionalParams[i];
            sourceBuilder.Append($", [CallerArgumentExpression(nameof({param.Name}))] string? {param.Name}Expression = null");
        }

        sourceBuilder.Append(")");

        // Add type constraints on new line if any
        var allConstraints = new List<string>(typeConstraints);
        if (genericConstraint != null)
        {
            allConstraints.Add(genericConstraint);
        }

        if (allConstraints.Count > 0)
        {
            sourceBuilder.AppendLine();
            sourceBuilder.Append($"        {string.Join(" ", allConstraints)}");
        }

        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    {");

        // Build expression string for error messages
        // Only include parameters that were actually provided (non-null expressions)
        sourceBuilder.Append($"        source.Context.ExpressionBuilder.Append(\".{methodName}(\"");
        if (additionalParams.Length > 0)
        {
            sourceBuilder.AppendLine();
            sourceBuilder.Append("            + string.Join(\", \", new[] { ");
            var expressionParts = additionalParams.Select(p => $"{p.Name}Expression");
            sourceBuilder.Append(string.Join(", ", expressionParts));
            sourceBuilder.Append(" }.Where(e => e != null))");
        }
        sourceBuilder.AppendLine(" + \")\");");

        // Construct and return the assertion
        sourceBuilder.Append($"        return new {assertionType.Name}");
        if (genericParams.Count > 0)
        {
            sourceBuilder.Append($"<{string.Join(", ", genericParams)}>");
        }
        sourceBuilder.Append("(");

        if (isNullableOverload)
        {
            sourceBuilder.Append("source.Context.AsNullable()");
        }
        else
        {
            sourceBuilder.Append("source.Context");
        }

        foreach (var param in additionalParams)
        {
            sourceBuilder.Append($", {param.Name}");
        }

        sourceBuilder.AppendLine(");");
        sourceBuilder.AppendLine("    }");
    }

    private static string FormatDefaultValue(object? defaultValue, ITypeSymbol type)
    {
        if (defaultValue == null)
        {
            return "null";
        }

        if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol enumType)
        {
            // Find the enum member that matches the default value
            foreach (var member in enumType.GetMembers())
            {
                if (member is IFieldSymbol { HasConstantValue: true } field &&
                    field.ConstantValue != null &&
                    field.ConstantValue.Equals(defaultValue))
                {
                    // Use just the enum name without namespace since we have using TUnit.Assertions.Enums;
                    return $"{enumType.Name}.{field.Name}";
                }
            }
            // Fallback if no matching member found
            return $"({enumType.ToDisplayString()})({defaultValue})";
        }

        if (defaultValue is string str)
        {
            return $"\"{str.Replace("\"", "\\\"")}\"";
        }

        if (defaultValue is bool b)
        {
            return b ? "true" : "false";
        }

        if (defaultValue is char c)
        {
            return $"'{c}'";
        }

        return defaultValue.ToString() ?? "null";
    }

    private record AssertionExtensionData(
        INamedTypeSymbol ClassSymbol,
        string MethodName,
        string? NegatedMethodName,
        INamedTypeSymbol AssertionBaseType,
        ImmutableArray<IMethodSymbol> Constructors,
        int OverloadResolutionPriority,
        string? RequiresUnreferencedCodeMessage
    );
}
