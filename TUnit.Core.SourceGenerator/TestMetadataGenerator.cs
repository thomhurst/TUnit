using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Core.SourceGenerator;

/// <summary>
/// Source generator that emits TestMetadata for discovered tests.
/// </summary>
[Generator]
public class TestMetadataGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all test methods
        var testMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.TestAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => GetTestMethodMetadata(ctx))
            .Where(static m => m is not null);

        // Generate a separate file for each test method to avoid Collect()
        context.RegisterSourceOutput(testMethods, GenerateTestRegistration);
    }

    private static TestMethodInfo? GetTestMethodMetadata(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        var typeSymbol = methodSymbol.ContainingType;

        // Skip abstract classes, static methods, and open generic types
        if (typeSymbol.IsAbstract || methodSymbol.IsStatic || typeSymbol is { IsGenericType: true, TypeParameters.Length: > 0 })
        {
            return null;
        }

        // Skip non-public methods
        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        // Get location info
        var location = context.TargetNode.GetLocation();
        var filePath = location.SourceTree?.FilePath ?? "";
        var lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;

        return new TestMethodInfo
        {
            MethodSymbol = methodSymbol,
            TypeSymbol = typeSymbol,
            FilePath = filePath,
            LineNumber = lineNumber,
            TestAttribute = context.Attributes[0]
        };
    }

    private static void GenerateTestRegistration(SourceProductionContext context, TestMethodInfo? testInfo)
    {
        if (testInfo == null)
        {
            return;
        }

        using var writer = new CodeWriter();

        // Generate a unique identifier for this test
        var guid = Guid.NewGuid().ToString("N");
        var className = GetFullTypeName(testInfo.TypeSymbol);
        var methodName = testInfo.MethodSymbol.Name;
        // Sanitize class and method names for use in filenames
        var safeClassName = SanitizeForFilename(className);
        var safeMethodName = SanitizeForFilename(methodName);

        // Check for required properties
        var requiredProperties = testInfo.TypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.IsRequired)
            .ToList();

        // Check for constructor parameters
        var constructors = testInfo.TypeSymbol.Constructors
            .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
            .OrderBy(c => c.Parameters.Length)
            .ToList();

        var hasParameterlessConstructor = constructors.Any(c => c.Parameters.Length == 0);
        var constructorWithParameters = !hasParameterlessConstructor ? constructors.FirstOrDefault() : null;

        writer.AppendLine("#nullable enable");
        writer.AppendLine("#pragma warning disable CS9113 // Parameter is unread.");
        writer.AppendLine();
        writer.AppendLine("using System;");
        writer.AppendLine("using System.Collections.Generic;");
        writer.AppendLine("using System.Linq;");
        writer.AppendLine("using System.Reflection;");
        writer.AppendLine("using global::TUnit.Core;");
        writer.AppendLine();
        writer.AppendLine("using global::TUnit.Core.SourceGenerator;");
        writer.AppendLine();
        writer.AppendLine("namespace TUnit.Generated;");
        writer.AppendLine();
        using (writer.BeginBlock($"internal static class TestMetadataRegistry_{safeClassName}_{safeMethodName}_{guid}"))
        {
            writer.AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]");
            using (writer.BeginBlock("public static void Initialize()"))
            {
                writer.AppendLine("var testMetadata = new System.Collections.Generic.List<DynamicTestMetadata>();");
                writer.AppendLine();
                // Extract skip information
                var (isSkipped, skipReason) = CodeGenerationHelpers.ExtractSkipInfo(testInfo.MethodSymbol);

                // For generic types, we can't use typeof() so TestClassType will be null
                var testClassTypeValue = testInfo.TypeSymbol.IsGenericType ? "null" : $"typeof({className})";

                // Create the test metadata object first without problematic array properties
                using (writer.BeginObjectInitializer("var metadata = new DynamicTestMetadata"))
                {
                    writer.AppendLine($"TestIdTemplate = \"{className}.{methodName}_{{{{TestIndex}}}}\",");
                    writer.AppendLine($"TestClassTypeReference = {CodeGenerationHelpers.GenerateTypeReference(testInfo.TypeSymbol)},");
                    writer.AppendLine($"TestClassType = {testClassTypeValue},");

                    using (writer.BeginObjectInitializer("MethodMetadata = new MethodMetadata", ","))
                    {
                        writer.AppendLine($"Name = \"{testInfo.MethodSymbol.Name}\",");
                        writer.AppendLine($"Type = {testClassTypeValue} ?? typeof(object),");
                        writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(testInfo.TypeSymbol)},");
                        writer.AppendLine($"Parameters = {CodeGenerationHelpers.GenerateParameterMetadataArray(testInfo.MethodSymbol)},");
                        writer.AppendLine($"GenericTypeCount = {testInfo.MethodSymbol.TypeParameters.Length},");

                        using (writer.BeginObjectInitializer("Class = new ClassMetadata", ","))
                        {
                            writer.AppendLine($"Name = \"{testInfo.TypeSymbol.Name}\",");
                            writer.AppendLine($"Type = {testClassTypeValue} ?? typeof(object),");
                            writer.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(testInfo.TypeSymbol)},");
                            writer.AppendLine($"Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(testInfo.TypeSymbol.GetAttributes(), testInfo.TypeSymbol)},");
                            writer.AppendLine($"Namespace = \"{testInfo.TypeSymbol.ContainingNamespace}\",");
                            writer.AppendLine($"Assembly = new AssemblyMetadata {{ Name = \"{testInfo.TypeSymbol.ContainingAssembly.Name}\", Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(testInfo.TypeSymbol.ContainingAssembly.GetAttributes(), testInfo.TypeSymbol.ContainingAssembly)} }},");
                            writer.AppendLine("Parameters = System.Array.Empty<ParameterMetadata>(),");
                            writer.AppendLine($"Properties = {CodeGenerationHelpers.GeneratePropertyMetadataArray(testInfo.TypeSymbol)},");
                            writer.AppendLine($"Constructors = {CodeGenerationHelpers.GenerateConstructorMetadataArray(testInfo.TypeSymbol)},");
                            writer.AppendLine("Parent = null");
                        }
                        writer.AppendLine($"ReturnType = {(ContainsTypeParameter(testInfo.MethodSymbol.ReturnType) ? "null" : $"typeof({GetReturnTypeName(testInfo.MethodSymbol)})")},");
                        writer.AppendLine($"ReturnTypeReference = {CodeGenerationHelpers.GenerateTypeReference(testInfo.MethodSymbol.ReturnType)},");
                        writer.AppendLine($"Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(testInfo.MethodSymbol.GetAttributes(), testInfo.MethodSymbol)}");
                    }
                    writer.AppendLine($"TestFilePath = @\"{testInfo.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
                    writer.AppendLine($"TestLineNumber = {testInfo.LineNumber},");
                    writer.AppendLine($"TestClassFactory = {GenerateTestClassFactory(testInfo.TypeSymbol, className, requiredProperties, constructorWithParameters, hasParameterlessConstructor)},");
                    writer.AppendLine($"ClassDataSources = {CodeGenerationHelpers.GenerateClassDataSourceProviders(testInfo.TypeSymbol)},");
                    writer.AppendLine($"MethodDataSources = {CodeGenerationHelpers.GenerateMethodDataSourceProviders(testInfo.MethodSymbol)},");
                    writer.AppendLine($"PropertyDataSources = {CodeGenerationHelpers.GeneratePropertyDataSourceDictionary(testInfo.TypeSymbol)},");
                    writer.AppendLine($"DisplayNameTemplate = \"{methodName}\",");
                    writer.AppendLine($"RepeatCount = {CodeGenerationHelpers.ExtractRepeatCount(testInfo.MethodSymbol)},");
                    writer.AppendLine($"IsAsync = {(IsAsyncMethod(testInfo.MethodSymbol) ? "true" : "false")},");
                    writer.AppendLine($"IsSkipped = {(isSkipped ? "true" : "false")},");
                    writer.AppendLine($"SkipReason = {skipReason},");
                    writer.AppendLine($"Timeout = {CodeGenerationHelpers.ExtractTimeout(testInfo.MethodSymbol)}");
                }
                writer.AppendLine();
                writer.AppendLine("testMetadata.Add(metadata);");
                writer.AppendLine();
                writer.AppendLine("TestSourceRegistrar.RegisterTests(testMetadata.Cast<ITestDescriptor>().ToList());");
            }
        }

        // Use GUID in the filename to prevent overwrites
        context.AddSource($"TestMetadata_{safeClassName}_{safeMethodName}_{guid}.g.cs", writer.ToString());
    }

    private static string GetFullTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
    }

    private static string SanitizeForFilename(string name)
    {
        // Replace all invalid filename characters with underscores
        var invalid = System.IO.Path.GetInvalidFileNameChars()
            .Concat(['<', '>', '(', ')', '[', ']', '{', '}', ',', ' ', '`', '.'])
            .Distinct();

        var sanitized = name;
        foreach (var c in invalid)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        return sanitized;
    }

    private static string GetReturnTypeName(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;
        if (returnType.SpecialType == SpecialType.System_Void)
        {
            return "void";
        }

        return returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
    }

    private static bool IsAsyncMethod(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;
        return returnType.Name == "Task" || returnType.Name == "ValueTask";
    }

    private static string GenerateTestClassFactory(INamedTypeSymbol typeSymbol, string className, List<IPropertySymbol> requiredProperties, IMethodSymbol? constructorWithParameters, bool hasParameterlessConstructor)
    {
        // For generic types, we can't create instances at compile time
        if (typeSymbol.IsGenericType)
        {
            return "null!"; // Will be replaced at runtime by TestBuilder
        }

        // If there are required properties with data sources, we need special handling
        var requiredPropertiesWithDataSource = requiredProperties
            .Where(p => p.GetAttributes().Any(attr =>
            {
                var attrClass = attr.AttributeClass;
                if (attrClass == null) return false;

                var fullName = attrClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return fullName == WellKnownFullyQualifiedClassNames.ClassDataSourceAttribute.WithGlobalPrefix ||
                       fullName == WellKnownFullyQualifiedClassNames.MethodDataSourceAttribute.WithGlobalPrefix ||
                       fullName == WellKnownFullyQualifiedClassNames.DataSourceGeneratorAttribute.WithGlobalPrefix ||
                       fullName == WellKnownFullyQualifiedClassNames.AsyncDataSourceGeneratorAttribute.WithGlobalPrefix;
            }))
            .ToList();

        // If there are any required properties, we need special handling
        // Generate a factory that creates the instance with required properties initialized
        if (requiredProperties.Any())
        {
            // Use a factory that creates the instance with required properties set to default values
            // The runtime will replace these with actual values if they have data sources
            return GenerateFactoryWithRequiredProperties(typeSymbol, className, requiredProperties, constructorWithParameters, hasParameterlessConstructor);
        }

        using var writer = new CodeWriter("", includeHeader: false); // No header for inline
        writer.Append("args => ");

        // If the class has a constructor with parameters and no parameterless constructor
        if (constructorWithParameters != null && !hasParameterlessConstructor)
        {
            // Use the args parameter which contains class constructor arguments
            writer.Append($"new {className}(");

            // Generate argument list with proper type casting
            var parameterList = string.Join(", ", constructorWithParameters.Parameters
                .Select((param, i) =>
                {
                    var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                    return $"({typeName})args[{i}]";
                }));

            writer.Append(parameterList);
            writer.Append(")");
        }
        else
        {
            // Simple parameterless constructor
            writer.Append($"new {className}()");
        }

        return writer.ToString().Trim(); // Trim to remove any extra newlines
    }

    private static string GenerateFactoryWithRequiredProperties(INamedTypeSymbol typeSymbol, string className, List<IPropertySymbol> requiredProperties, IMethodSymbol? constructorWithParameters, bool hasParameterlessConstructor)
    {
        using var writer = new CodeWriter("", includeHeader: false);
        // No need to indent here - this generates inline code
        writer.Append("args => ");

        // Create a new instance with all required properties initialized
        if (constructorWithParameters != null && !hasParameterlessConstructor)
        {
            writer.Append($"new {className}(");
            var parameterList = string.Join(", ", constructorWithParameters.Parameters
                .Select((param, i) =>
                {
                    var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                    return $"({typeName})args[{i}]";
                }));
            writer.Append(parameterList);
            writer.Append(")");
        }
        else
        {
            writer.Append($"new {className}()");
        }

        // Always add object initializer for required properties
        writer.Append(" { ");
        var propertyInitializers = requiredProperties.Select(prop =>
        {
            // For properties with data sources, create a minimal valid instance
            // that satisfies the compiler but will be replaced at runtime
            var defaultValue = GetDataSourceAwareDefaultValue(prop);
            return $"{prop.Name} = {defaultValue}";
        });
        writer.Append(string.Join(", ", propertyInitializers));
        writer.Append(" }");

        return writer.ToString().Trim();
    }

    private static string GetDataSourceAwareDefaultValue(IPropertySymbol property)
    {
        var type = property.Type;

        // For reference types, try to create a new instance
        if (type.IsReferenceType)
        {
            var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

            // Special handling for common types
            if (type.SpecialType == SpecialType.System_String)
            {
                return "string.Empty";
            }

            // Check if the type has a parameterless constructor
            if (type is INamedTypeSymbol namedType)
            {
                var hasParameterlessConstructor = namedType.Constructors
                    .Any(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length == 0);

                if (hasParameterlessConstructor)
                {
                    return $"new {typeName}()";
                }
            }

            // Fallback to null with suppression
            return "null!";
        }

        // Use the existing logic for value types
        return GetDefaultValueForType(type);
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

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            return namedType.TypeArguments.Any(ContainsTypeParameter);
        }

        return false;
    }

    private static string GetDefaultValueForType(ITypeSymbol type)
    {
        if (type.IsReferenceType || type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return "null!";
        }

        return type.SpecialType switch
        {
            SpecialType.System_Boolean => "false",
            SpecialType.System_Char => "'\\0'",
            SpecialType.System_SByte => "(sbyte)0",
            SpecialType.System_Byte => "(byte)0",
            SpecialType.System_Int16 => "(short)0",
            SpecialType.System_UInt16 => "(ushort)0",
            SpecialType.System_Int32 => "0",
            SpecialType.System_UInt32 => "0u",
            SpecialType.System_Int64 => "0L",
            SpecialType.System_UInt64 => "0ul",
            SpecialType.System_Decimal => "0m",
            SpecialType.System_Single => "0f",
            SpecialType.System_Double => "0d",
            _ => $"default({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))})"
        };
    }

    private class TestMethodInfo
    {
        public IMethodSymbol MethodSymbol { get; set; } = null!;
        public INamedTypeSymbol TypeSymbol { get; set; } = null!;
        public string FilePath { get; set; } = "";
        public int LineNumber { get; set; }
        public AttributeData TestAttribute { get; set; } = null!;
    }
}
