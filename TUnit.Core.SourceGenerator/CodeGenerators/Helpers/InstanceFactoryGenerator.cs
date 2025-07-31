using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class InstanceFactoryGenerator
{
    public static void GenerateInstanceFactory(CodeWriter writer, ITypeSymbol typeSymbol)
    {
        GenerateInstanceFactory(writer, typeSymbol, null);
    }

    public static void GenerateInstanceFactory(CodeWriter writer, ITypeSymbol typeSymbol, TestMethodMetadata? testMethod)
    {
        var className = typeSymbol.GloballyQualified();

        // Check if the class has a ClassConstructor attribute
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            var hasClassConstructor = namedTypeSymbol.GetAttributesIncludingBaseTypes()
                .Any(a => a.AttributeClass?.GloballyQualifiedNonGeneric() == WellKnownFullyQualifiedClassNames.ClassConstructorAttribute.WithGlobalPrefix);

            if (hasClassConstructor)
            {
                // If class has ClassConstructor attribute, generate a factory that throws
                // The actual instance creation will be handled by ClassConstructorHelper at runtime
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
        var constructors = typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic)
            .OrderByDescending(c => c.Parameters.Length)
            .ToList();

        if (constructors.Count == 1)
        {
            return constructors[0];
        }

        var publicConstructors = constructors.Where(c => c.DeclaredAccessibility == Accessibility.Public).ToList();
        return publicConstructors.Count == 1 ? publicConstructors[0] : publicConstructors.FirstOrDefault();
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

            // Check if we have class data sources that might need AOT conversion
            var classDataSources = GetClassDataSources(testMethod);
            
            // Generate constructor arguments with AOT conversion support
            var parameterTypes = constructor.Parameters.Select(p => p.Type).ToList();
            
            for (var i = 0; i < parameterTypes.Count; i++)
            {
                if (i > 0)
                {
                    writer.Append(", ");
                }
                
                var parameterType = parameterTypes[i];
                var argAccess = $"args[{i}]";
                
                // Check if this parameter might receive a value from a class data source with conversion operators
                if (classDataSources.Any() && ShouldUseAotConversion(classDataSources, i, parameterType))
                {
                    // Generate AOT-compatible conversion
                    writer.Append(GenerateAotConstructorArgument(parameterType, argAccess, classDataSources, i));
                }
                else
                {
                    // Use regular CastHelper
                    writer.Append($"TUnit.Core.Helpers.CastHelper.Cast<{parameterType.GloballyQualified()}>({argAccess})");
                }
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

    private static List<AttributeData> GetClassDataSources(TestMethodMetadata? testMethod)
    {
        if (testMethod == null)
        {
            return new List<AttributeData>();
        }

        // Get class data source attributes from the test type
        return testMethod.TypeSymbol.GetAttributesIncludingBaseTypes()
            .Where(a => a.AttributeClass?.Name == "ClassDataSourceAttribute")
            .ToList();
    }

    private static bool ShouldUseAotConversion(List<AttributeData> classDataSources, int parameterIndex, ITypeSymbol targetType)
    {
        // Check if any class data source at this index has a type with conversion operators
        foreach (var attr in classDataSources)
        {
            if (attr.AttributeClass == null)
                continue;

            // For non-generic ClassDataSourceAttribute, check the constructor argument
            if (!attr.AttributeClass.IsGenericType && attr.ConstructorArguments.Length > parameterIndex)
            {
                var sourceType = attr.ConstructorArguments[parameterIndex].Value as ITypeSymbol;
                if (sourceType != null && AotConversionHelper.HasConversionOperators(sourceType))
                {
                    return true;
                }
            }
            // For generic ClassDataSourceAttribute<T>, check the type argument
            else if (attr.AttributeClass.IsGenericType && attr.AttributeClass.TypeArguments.Length > parameterIndex)
            {
                var sourceType = attr.AttributeClass.TypeArguments[parameterIndex];
                if (AotConversionHelper.HasConversionOperators(sourceType))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    private static string GenerateAotConstructorArgument(ITypeSymbol targetType, string argAccess, List<AttributeData> classDataSources, int parameterIndex)
    {
        // Try to find the source type from class data sources
        ITypeSymbol? sourceType = null;
        
        foreach (var attr in classDataSources)
        {
            if (attr.AttributeClass == null)
                continue;

            // For non-generic ClassDataSourceAttribute
            if (!attr.AttributeClass.IsGenericType && attr.ConstructorArguments.Length > parameterIndex)
            {
                sourceType = attr.ConstructorArguments[parameterIndex].Value as ITypeSymbol;
                if (sourceType != null)
                    break;
            }
            // For generic ClassDataSourceAttribute<T>
            else if (attr.AttributeClass.IsGenericType && attr.AttributeClass.TypeArguments.Length > parameterIndex)
            {
                sourceType = attr.AttributeClass.TypeArguments[parameterIndex];
                if (sourceType != null)
                    break;
            }
        }

        if (sourceType != null)
        {
            // Generate AOT-compatible conversion check
            var conversionExpression = AotConversionHelper.GenerateAotConversion(sourceType, targetType, argAccess);
            if (conversionExpression != null)
            {
                // Wrap in a runtime type check for safety
                return $"({argAccess} is {sourceType.GloballyQualified()} ? {conversionExpression} : TUnit.Core.Helpers.CastHelper.Cast<{targetType.GloballyQualified()}>({argAccess}))";
            }
        }

        // Fallback to CastHelper
        return $"TUnit.Core.Helpers.CastHelper.Cast<{targetType.GloballyQualified()}>({argAccess})";
    }
}
