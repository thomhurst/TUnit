using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

[Generator]
public class InheritsTestsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classesWithInheritsTests = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.InheritsTestsAttribute",
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetInheritsTestsClassInfo(ctx))
            .Where(static m => m is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(classesWithInheritsTests.Collect());

        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(spc, source.Left, source.Right!));
    }

    private static InheritsTestsClassInfo? GetInheritsTestsClassInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        // Skip if class is abstract
        if (classSymbol.IsAbstract)
        {
            return null;
        }

        // Get location info
        var location = context.TargetNode.GetLocation();
        var filePath = location.SourceTree?.FilePath ?? "";
        var lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;

        return new InheritsTestsClassInfo
        {
            ClassSymbol = classSymbol,
            FilePath = filePath,
            LineNumber = lineNumber
        };
    }

    private static void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<InheritsTestsClassInfo?> classInfos)
    {
        var testAttributeSymbol = compilation.GetTypeByMetadataName("TUnit.Core.TestAttribute");
        if (testAttributeSymbol == null)
        {
            return;
        }

        foreach (var classInfo in classInfos)
        {
            if (classInfo == null)
            {
                continue;
            }

            var classSymbol = classInfo.ClassSymbol;
            var testMethods = new List<InheritedTestMethodInfo>();

            // Collect test methods from all base classes
            CollectTestMethodsFromBaseClasses(classSymbol, testAttributeSymbol, testMethods);

            if (testMethods.Count == 0)
            {
                continue;
            }

            // Generate test metadata for each inherited test method
            foreach (var testMethod in testMethods)
            {
                var sourceCode = GenerateTestMetadataForMethod(classInfo, testMethod);
                var guid = Guid.NewGuid().ToString("N");
                var safeClassName = SanitizeForFilename(classSymbol.ToDisplayString());
                var safeMethodName = SanitizeForFilename(testMethod.Method.Name);
                var fileName = $"TestMetadata_{safeClassName}_{safeMethodName}_{guid}.g.cs";

                context.AddSource(fileName, sourceCode);
            }
        }
    }

    private static void CollectTestMethodsFromBaseClasses(
        INamedTypeSymbol derivedClass,
        INamedTypeSymbol testAttributeSymbol,
        List<InheritedTestMethodInfo> testMethods)
    {
        var currentType = derivedClass.BaseType;

        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            // Skip if base type is an open generic type (has unbound type parameters)
            // But allow closed/constructed generic types like GenericTestExample<int>
            if (currentType.IsUnboundGenericType ||
                (currentType.IsGenericType && currentType.TypeArguments.Any(arg => arg.TypeKind == TypeKind.TypeParameter)))
            {
                // Skip open generic types - they can't be instantiated
                currentType = currentType.BaseType;
                continue;
            }

            // Get all methods from the base class
            var methods = currentType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => !m.IsStatic && m.MethodKind == MethodKind.Ordinary);

            foreach (var method in methods)
            {
                // Check if method has Test attribute
                var testAttribute = method.GetAttributes()
                    .FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, testAttributeSymbol));

                if (testAttribute != null)
                {
                    testMethods.Add(new InheritedTestMethodInfo
                    {
                        Method = method,
                        BaseClass = currentType,
                        TestAttribute = testAttribute
                    });
                }
            }

            currentType = currentType.BaseType;
        }
    }

    private static string GenerateTestMetadataForMethod(InheritsTestsClassInfo classInfo, InheritedTestMethodInfo testMethodInfo)
    {
        using var writer = new CodeWriter();
        var classSymbol = classInfo.ClassSymbol;
        var methodSymbol = testMethodInfo.Method;
        var baseClass = testMethodInfo.BaseClass;

        // Generate unique registry class name
        var guid = Guid.NewGuid();
        var registryClassName = $"TestMetadataRegistry_{classSymbol.Name}_{methodSymbol.Name}_{guid:N}";

        writer.AppendLine("#nullable enable");
        writer.AppendLine("#pragma warning disable CS9113 // Parameter is unread.");
        writer.AppendLine();
        writer.AppendLine("using System;");
        writer.AppendLine("using System.Collections.Generic;");
        writer.AppendLine("using System.Linq;");
        writer.AppendLine("using System.Reflection;");
        writer.AppendLine("using global::TUnit.Core;");
        writer.AppendLine("using global::TUnit.Core.SourceGenerator;");
        writer.AppendLine();
        writer.AppendLine("namespace TUnit.Generated;");
        writer.AppendLine();
        writer.AppendBlock($"internal static class {registryClassName}", w =>
        {
            w.AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]");
            w.AppendBlock("public static void Initialize()", w2 =>
            {
                w2.AppendLine("var testMetadata = new System.Collections.Generic.List<DynamicTestMetadata>();");
                w2.AppendLine();

                // Generate test metadata
                w2.AppendLine("// Create the test metadata object first without problematic array properties");
                using (w2.BeginObjectInitializer("var metadata = new DynamicTestMetadata", ";"))
                {
                    w2.AppendLine($"TestIdTemplate = \"{GetFullTypeName(classSymbol)}.{methodSymbol.Name}_{{{{TestIndex}}}}\",");
                    w2.AppendLine($"TestClassTypeReference = {CodeGenerationHelpers.GenerateTypeReference(classSymbol)},");
                    w2.AppendLine($"TestClassType = typeof({GetFullTypeName(classSymbol)}),");

                    // Generate TestClassFactory
                    w2.Append("TestClassFactory = ");
                    w2.Append(GenerateTestClassFactory(classSymbol));
                    w2.AppendLine(",");

                    // Generate MethodMetadata
                    w2.AppendBlock("MethodMetadata = new MethodMetadata", w3 =>
                    {
                        w3.AppendLine($"Name = \"{methodSymbol.Name}\",");
                        
                        // For methods on generic types, use the concrete derived type
                        if (baseClass.IsGenericType)
                        {
                            w3.AppendLine($"Type = typeof({GetFullTypeName(classSymbol)}) ?? typeof(object),");
                        }
                        else
                        {
                            w3.AppendLine($"Type = typeof({GetFullTypeName(baseClass)}) ?? typeof(object),");
                        }
                        
                        w3.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(baseClass)},");
                        w3.AppendLine($"Parameters = {CodeGenerationHelpers.GenerateParameterMetadataArray(methodSymbol)},");
                        w3.AppendLine($"GenericTypeCount = {methodSymbol.TypeParameters.Length},");
                        w3.AppendLine($"ReturnTypeReference = {CodeGenerationHelpers.GenerateTypeReference(methodSymbol.ReturnType)},");
                        w3.AppendLine($"ReturnType = {(ContainsTypeParameter(methodSymbol.ReturnType) ? "null" : $"typeof({GetReturnTypeName(methodSymbol)})!")},");

                        // Generate ClassMetadata
                        w3.AppendBlock("Class = new ClassMetadata", w4 =>
                        {
                            w4.AppendLine($"Name = \"{classSymbol.Name}\",");
                            w4.AppendLine($"Type = typeof({GetFullTypeName(classSymbol)}) ?? typeof(object),");
                            w4.AppendLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(classSymbol)},");
                            w4.AppendLine($"Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(classSymbol.GetAttributes(), classSymbol, w3.IndentLevel)},");
                            w4.AppendLine($"Namespace = \"{classSymbol.ContainingNamespace?.ToDisplayString() ?? ""}\",");

                            // Generate Assembly metadata
                            w4.AppendBlock("Assembly = new AssemblyMetadata", w5 =>
                            {
                                w5.AppendLine($"Name = \"{classSymbol.ContainingAssembly.Name}\",");
                                w5.AppendLine($"Attributes = System.Array.Empty<global::TUnit.Core.AttributeMetadata>()");
                            });
                            w4.AppendLine(",");

                            // Generate Parameters (for class constructor)
                            w4.AppendLine($"Parameters = System.Array.Empty<global::TUnit.Core.ParameterMetadata>(),");
                            w4.AppendLine($"Properties = {CodeGenerationHelpers.GeneratePropertyMetadataArray(classSymbol)},");
                            w4.AppendLine("Parent = null");
                        });
                        w3.AppendLine(",");
                        w3.AppendLine($"Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(methodSymbol.GetAttributes(), methodSymbol, w3.IndentLevel)},");
                        
                        // For methods on generic types, we need to look up the method on the base type where it's declared
                        // The runtime will handle finding the correct method on the derived type
                        if (baseClass.IsGenericType)
                        {
                            // For generic base types, we need to construct the closed generic type and then find the method
                            var closedBaseType = GetClosedGenericTypeName(baseClass, classSymbol);
                            var paramTypesArray = GenerateParameterTypesArray(methodSymbol);
                            if (paramTypesArray == "null")
                            {
                                // For methods with generic parameters, use GetMethods and find by name
                                w3.AppendLine($"ReflectionInformation = typeof({closedBaseType}).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(m => m.Name == \"{methodSymbol.Name}\" && m.GetParameters().Length == {methodSymbol.Parameters.Length})");
                            }
                            else
                            {
                                w3.AppendLine($"ReflectionInformation = typeof({closedBaseType}).GetMethod(\"{methodSymbol.Name}\", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, {paramTypesArray}, null)");
                            }
                        }
                        else
                        {
                            var paramTypesArray = GenerateParameterTypesArray(methodSymbol);
                            if (paramTypesArray == "null")
                            {
                                // For methods with generic parameters, use GetMethods and find by name
                                w3.AppendLine($"ReflectionInformation = typeof({GetFullTypeName(baseClass)}).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(m => m.Name == \"{methodSymbol.Name}\" && m.GetParameters().Length == {methodSymbol.Parameters.Length})");
                            }
                            else
                            {
                                w3.AppendLine($"ReflectionInformation = typeof({GetFullTypeName(baseClass)}).GetMethod(\"{methodSymbol.Name}\", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, {paramTypesArray}, null)");
                            }
                        }
                    });
                    w2.AppendLine(",");

                    w2.AppendLine($"DisplayNameTemplate = \"{GetDisplayName(classSymbol, methodSymbol, testMethodInfo.TestAttribute)}\",");
                    w2.AppendLine($"TestFilePath = \"{classInfo.FilePath.Replace("\\", "\\\\")}\",");
                    w2.AppendLine($"TestLineNumber = {classInfo.LineNumber},");
                    w2.AppendLine($"ClassDataSources = {CodeGenerationHelpers.GenerateClassDataSourceProviders(classSymbol)},");
                    w2.AppendLine($"MethodDataSources = {CodeGenerationHelpers.GenerateMethodDataSourceProviders(methodSymbol)},");
                    w2.AppendLine($"PropertyDataSources = new System.Collections.Generic.Dictionary<System.Reflection.PropertyInfo, global::TUnit.Core.Interfaces.IDataSource>(),");

                    // Extract timeout, skip info, and repeat count
                    var (isSkipped, skipReason) = CodeGenerationHelpers.ExtractSkipInfo(methodSymbol);
                    w2.AppendLine($"IsSkipped = {(isSkipped ? "true" : "false")},");
                    w2.AppendLine($"SkipReason = {skipReason},");
                    w2.AppendLine($"Timeout = {CodeGenerationHelpers.ExtractTimeout(methodSymbol)},");
                    w2.AppendLine($"RepeatCount = {CodeGenerationHelpers.ExtractRepeatCount(methodSymbol)},");
                    w2.AppendLine($"IsAsync = {(IsAsyncMethod(methodSymbol) ? "true" : "false")}");
                }
                w2.AppendLine();
                w2.AppendLine("testMetadata.Add(metadata);");

                w2.AppendLine();
                w2.AppendLine("TestSourceRegistrar.RegisterTests(testMetadata.Cast<ITestDescriptor>().ToList());");
            });
        });

        return writer.ToString();
    }

    private static string GetFullTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
    }
    
    private static string GetClosedGenericTypeName(INamedTypeSymbol genericBaseClass, INamedTypeSymbol derivedClass)
    {
        // For a generic base class like GenericTestExample<T>, and a derived class like IntGenericTests : GenericTestExample<int>
        // We need to construct the closed generic type name like GenericTestExample<int>
        
        // Find the base type in the derived class hierarchy that matches our generic base class
        var currentType = derivedClass.BaseType;
        while (currentType != null)
        {
            if (currentType.OriginalDefinition.Equals(genericBaseClass.OriginalDefinition, SymbolEqualityComparer.Default))
            {
                // Found it - return the closed generic type
                return GetFullTypeName(currentType);
            }
            currentType = currentType.BaseType;
        }
        
        // Fallback - shouldn't happen
        return GetFullTypeName(genericBaseClass);
    }

    private static string GenerateTestClassFactory(INamedTypeSymbol classSymbol)
    {
        // For generic types, we can't create instances at compile time
        if (classSymbol.IsGenericType)
        {
            return "null!"; // Will be replaced at runtime by TestBuilder
        }

        // Check for required properties that don't have data source attributes in this class and all base classes
        var requiredProperties = GetAllRequiredProperties(classSymbol);

        // Check if all required properties have data sources
        var requiredPropertiesWithDataSource = requiredProperties
            .Where(p => p.GetAttributes().Any(attr =>
            {
                var attrName = attr.AttributeClass?.Name ?? "";
                return attrName.EndsWith("DataSource") || attrName.EndsWith("DataSourceAttribute");
            }))
            .ToList();

        // If there are any required properties, generate special factory
        if (requiredProperties.Any())
        {
            return GenerateFactoryWithRequiredProperties(classSymbol, requiredProperties);
        }

        using var writer = new CodeWriter(includeHeader: false);
        writer.Append("args => ");

        // Find appropriate constructor
        var constructors = classSymbol.Constructors
            .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
            .OrderBy(c => c.Parameters.Length)
            .ToList();

        var hasParameterlessConstructor = constructors.Any(c => c.Parameters.Length == 0);
        var constructorWithParameters = !hasParameterlessConstructor ? constructors.FirstOrDefault() : null;

        var className = GetFullTypeName(classSymbol);

        // If the class has a constructor with parameters and no parameterless constructor
        if (constructorWithParameters != null && !hasParameterlessConstructor)
        {
            // Use the args parameter which contains class constructor arguments
            writer.Append($"new {className}(");

            // Generate argument list with proper type casting
            var parameterList = string.Join(", ", constructorWithParameters.Parameters
                .Select((param, i) =>
                {
                    var typeName = GetFullTypeName(param.Type);
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

    private static List<IPropertySymbol> GetAllRequiredProperties(INamedTypeSymbol classSymbol)
    {
        var requiredProperties = new List<IPropertySymbol>();
        var currentType = classSymbol;

        while (currentType != null)
        {
            var properties = currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.IsRequired);

            requiredProperties.AddRange(properties);
            currentType = currentType.BaseType;
        }

        return requiredProperties;
    }

    private static string GenerateFactoryWithRequiredProperties(INamedTypeSymbol classSymbol, List<IPropertySymbol> requiredProperties)
    {
        using var writer = new CodeWriter(includeHeader: false);
        writer.Append("args => ");

        var className = GetFullTypeName(classSymbol);

        // Find appropriate constructor
        var constructors = classSymbol.Constructors
            .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
            .OrderBy(c => c.Parameters.Length)
            .ToList();

        var hasParameterlessConstructor = constructors.Any(c => c.Parameters.Length == 0);
        var constructorWithParameters = !hasParameterlessConstructor ? constructors.FirstOrDefault() : null;

        // Create a new instance with constructor parameters if needed
        if (constructorWithParameters != null && !hasParameterlessConstructor)
        {
            writer.Append($"new {className}(");
            var parameterList = string.Join(", ", constructorWithParameters.Parameters
                .Select((param, i) =>
                {
                    var typeName = GetFullTypeName(param.Type);
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
            var typeName = GetFullTypeName(type);

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

    private static List<IPropertySymbol> GetAllRequiredPropertiesWithoutDataSource(INamedTypeSymbol classSymbol)
    {
        var requiredProperties = new List<IPropertySymbol>();
        var currentType = classSymbol;

        // Walk up the inheritance chain to find all required properties
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            var typeRequiredProperties = currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.IsRequired && !HasDataSourceAttribute(p));

            requiredProperties.AddRange(typeRequiredProperties);
            currentType = currentType.BaseType;
        }

        return requiredProperties;
    }

    private static bool HasDataSourceAttribute(IPropertySymbol property)
    {
        // Check if property has any attribute that ends with "DataSource" or "DataSourceAttribute"
        return property.GetAttributes().Any(attr =>
        {
            var attrName = attr.AttributeClass?.Name ?? "";
            return attrName.EndsWith("DataSource") || attrName.EndsWith("DataSourceAttribute");
        });
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
            SpecialType.System_SByte => "0",
            SpecialType.System_Byte => "0",
            SpecialType.System_Int16 => "0",
            SpecialType.System_UInt16 => "0",
            SpecialType.System_Int32 => "0",
            SpecialType.System_UInt32 => "0",
            SpecialType.System_Int64 => "0",
            SpecialType.System_UInt64 => "0",
            SpecialType.System_Decimal => "0m",
            SpecialType.System_Single => "0f",
            SpecialType.System_Double => "0d",
            _ => $"default({GetFullTypeName(type)})"
        };
    }

    private static string GetReturnTypeName(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;
        if (returnType.SpecialType == SpecialType.System_Void)
        {
            return "void";
        }

        return GetFullTypeName(returnType);
    }

    private static bool IsAsyncMethod(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;
        return returnType.Name == "Task" || returnType.Name == "ValueTask";
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

    private static string GetDisplayName(INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol, AttributeData testAttribute)
    {
        // Check for custom display name
        var displayNameArg = testAttribute.NamedArguments
            .FirstOrDefault(na => na.Key == "DisplayName");

        if (displayNameArg.Value.Value is string customDisplayName)
        {
            return customDisplayName;
        }

        // Default display name
        return $"{classSymbol.Name}.{methodSymbol.Name}";
    }

    private static string SanitizeForFilename(string input)
    {
        return input
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(".", "_")
            .Replace(",", "_")
            .Replace(" ", "_")
            .Replace("`", "_")
            .Replace("[", "_")
            .Replace("]", "_");
    }

    private static string GenerateParameterTypesArray(IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
        {
            return "System.Type.EmptyTypes";
        }
        
        // If any parameter contains type parameters, we can't generate the parameter types array
        // at compile time. The runtime will need to handle this.
        if (method.Parameters.Any(p => ContainsTypeParameter(p.Type)))
        {
            // Return null to indicate that parameter type matching should be done at runtime
            return "null";
        }
        
        var parameterTypes = method.Parameters
            .Select(p => $"typeof({GetFullTypeName(p.Type)})")
            .ToArray();
            
        return $"new System.Type[] {{ {string.Join(", ", parameterTypes)} }}";
    }

    private class InheritsTestsClassInfo
    {
        public INamedTypeSymbol ClassSymbol { get; set; } = null!;
        public string FilePath { get; set; } = "";
        public int LineNumber { get; set; }
    }

    private class InheritedTestMethodInfo
    {
        public IMethodSymbol Method { get; set; } = null!;
        public INamedTypeSymbol BaseClass { get; set; } = null!;
        public AttributeData TestAttribute { get; set; } = null!;
    }
}
