using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class InstanceFactoryGenerator
{
    /// <summary>
    /// Generates code to create an instance of a type with proper required property handling.
    /// This handles required properties that don't have data sources by initializing them with defaults.
    /// </summary>
    public static void GenerateInstanceCreation(CodeWriter writer, ITypeSymbol typeSymbol, string variableName)
    {
        var className = typeSymbol.GloballyQualified();
        // Use GetAllRequiredProperties because even properties with data sources need to be
        // initialized in the object initializer to satisfy C#'s required modifier constraint.
        // The actual values will be populated by the data sources at runtime.
        var requiredProperties = RequiredPropertyHelper.GetAllRequiredProperties(typeSymbol).ToArray();

        if (requiredProperties.Length == 0)
        {
            writer.AppendLine($"{variableName} = new {className}();");
        }
        else
        {
            writer.AppendLine($"{variableName} = new {className}()");
            writer.AppendLine("{");
            writer.Indent();
            foreach (var property in requiredProperties)
            {
                var defaultValue = RequiredPropertyHelper.GetDefaultValueForType(property.Type);
                writer.AppendLine($"{property.Name} = {defaultValue},");
            }
            writer.Unindent();
            writer.AppendLine("};");
        }
    }

    public static void GenerateInstanceFactory(CodeWriter writer, ITypeSymbol typeSymbol)
    {
        GenerateInstanceFactory(writer, typeSymbol, null);
    }

    public static void GenerateInstanceFactory(CodeWriter writer, ITypeSymbol typeSymbol, TestMethodMetadata? testMethod)
    {
        var className = typeSymbol.GloballyQualified();

        // Check if the class has a ClassConstructor attribute first, before any other checks
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            var hasClassConstructor = namedTypeSymbol.GetAttributesIncludingBaseTypes()
                .Any(a => a.AttributeClass?.GloballyQualifiedNonGeneric() == WellKnownFullyQualifiedClassNames.ClassConstructorAttribute.WithGlobalPrefix);

            if (hasClassConstructor)
            {
                // If class has ClassConstructor attribute, generate a factory that throws
                // The actual instance creation will be handled by ClassConstructorHelper at runtime
                // This applies to both generic and non-generic classes
                writer.AppendLine("InstanceFactory = (typeArgs, args) =>");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("// ClassConstructor attribute is present - instance creation handled at runtime");
                writer.AppendLine("throw new global::System.NotSupportedException(\"Instance creation for classes with ClassConstructor attribute is handled at runtime\");");
                writer.Unindent();
                writer.AppendLine("},");
                return;
            }
        }

        // Check if this is a generic type definition
        if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType && namedType.TypeArguments.Any(ta => ta is ITypeParameterSymbol))
        {
            // Generate factory that uses MakeGenericType
            GenerateGenericInstanceFactory(writer, namedType);
            return;
        }

        if (typeSymbol.HasParameterizedConstructor())
        {
            var constructor = GetPrimaryConstructor(typeSymbol);
            if (constructor != null)
            {
                GenerateTypedConstructorCall(writer, className, constructor, testMethod);
            }
            else
            {
                writer.AppendLine("InstanceFactory = null,");
            }
        }
        else
        {
            // For classes with default constructor, check for ALL required properties
            var requiredProperties = RequiredPropertyHelper.GetAllRequiredProperties(typeSymbol);

            if (!requiredProperties.Any())
            {
                writer.AppendLine($"InstanceFactory = (typeArgs, args) => new {className}(),");
            }
            else
            {
                writer.AppendLine($"InstanceFactory = (typeArgs, args) => new {className}()");
                writer.AppendLine("{");
                writer.Indent();

                foreach (var property in requiredProperties)
                {
                    var defaultValue = RequiredPropertyHelper.GetDefaultValueForType(property.Type);
                    writer.AppendLine($"{property.Name} = {defaultValue},");
                }

                writer.Unindent();
                writer.AppendLine("},");
            }
        }
    }

    private static IMethodSymbol? GetPrimaryConstructor(ITypeSymbol typeSymbol)
    {
        // Materialize constructors once to avoid multiple enumerations
        var constructors = typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic)
            .ToArray();

        // First, check for constructors marked with [TestConstructor]
        foreach (var constructor in constructors)
        {
            if (constructor.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString() == WellKnownFullyQualifiedClassNames.TestConstructorAttribute.WithoutGlobalPrefix))
            {
                return constructor;
            }
        }

        // If no [TestConstructor] found, use existing logic
        var orderedConstructors = constructors.OrderByDescending(c => c.Parameters.Length).ToArray();

        if (orderedConstructors.Length == 1)
        {
            return orderedConstructors[0];
        }

        var publicConstructors = orderedConstructors.Where(c => c.DeclaredAccessibility == Accessibility.Public).ToArray();
        return publicConstructors.Length == 1 ? publicConstructors[0] : publicConstructors.FirstOrDefault();
    }

    private static void GenerateTypedConstructorCall(CodeWriter writer, string className, IMethodSymbol constructor, TestMethodMetadata? testMethod)
    {
        writer.AppendLine("InstanceFactory = (typeArgs, args) =>");
        writer.AppendLine("{");
        writer.Indent();

        // Check for required properties
        var requiredProperties = RequiredPropertyHelper.GetAllRequiredProperties(constructor.ContainingType);

        if (constructor.Parameters.Length == 0 && !requiredProperties.Any())
        {
            writer.AppendLine($"return new {className}();");
        }
        else
        {
            writer.Append($"return new {className}(");

            // Generate constructor arguments
            var parameterTypes = constructor.Parameters.Select(p => p.Type).ToList();
            
            for (var i = 0; i < parameterTypes.Count; i++)
            {
                if (i > 0)
                {
                    writer.Append(", ");
                }
                
                var parameterType = parameterTypes[i];
                var argAccess = $"args[{i}]";
                
                // Use CastHelper which now has AOT converter registry support
                writer.Append($"global::TUnit.Core.Helpers.CastHelper.Cast<{parameterType.GloballyQualified()}>({argAccess})");
            }

            writer.Append(")");

            // Add object initializer for required properties
            if (requiredProperties.Any())
            {
                writer.AppendLine();
                writer.AppendLine("{");
                writer.Indent();

                foreach (var property in requiredProperties)
                {
                    var defaultValue = RequiredPropertyHelper.GetDefaultValueForType(property.Type);
                    writer.AppendLine($"{property.Name} = {defaultValue},");
                }

                writer.Unindent();
                writer.Append("}");
            }

            writer.AppendLine(";");
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    private static void GenerateGenericInstanceFactory(CodeWriter writer, INamedTypeSymbol genericType)
    {
        writer.AppendLine("InstanceFactory = (typeArgs, args) =>");
        writer.AppendLine("{");
        writer.Indent();

        // Get the open generic type
        writer.AppendLine($"var openGenericType = typeof({genericType.OriginalDefinition.GloballyQualified()});");
        writer.AppendLine();
        
        // Create the closed generic type
        writer.AppendLine("var closedGenericType = global::TUnit.Core.Helpers.GenericTypeHelper.MakeGenericTypeSafe(openGenericType, typeArgs);");
        writer.AppendLine();
        
        // Check for constructor parameters
        var constructor = GetPrimaryConstructor(genericType);
        if (constructor is { Parameters.Length: > 0 })
        {
            writer.AppendLine("// Create instance with constructor arguments");
            writer.AppendLine("return global::System.Activator.CreateInstance(closedGenericType, args);");
        }
        else
        {
            writer.AppendLine("// Create instance with parameterless constructor");
            writer.AppendLine("var instance = global::System.Activator.CreateInstance(closedGenericType);");
            
            // Check for required properties
            var requiredProperties = RequiredPropertyHelper.GetAllRequiredProperties(genericType);
            if (requiredProperties.Any())
            {
                writer.AppendLine();
                writer.AppendLine("// Set required properties");
                foreach (var property in requiredProperties)
                {
                    var defaultValue = RequiredPropertyHelper.GetDefaultValueForType(property.Type);
                    writer.AppendLine($"closedGenericType.GetProperty(\"{property.Name}\")?.SetValue(instance, {defaultValue});");
                }
            }
            
            writer.AppendLine("return instance!;");
        }

        writer.Unindent();
        writer.AppendLine("},");
    }
}
