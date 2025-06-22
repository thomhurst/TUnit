using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;

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
        using var writer = new SourceCodeWriter();
        var classSymbol = classInfo.ClassSymbol;
        var methodSymbol = testMethodInfo.Method;
        var baseClass = testMethodInfo.BaseClass;
        
        // Generate unique registry class name
        var registryClassName = $"TestMetadataRegistry_{classSymbol.Name}_{methodSymbol.Name}_{Guid.NewGuid():N}";
        
        writer.WriteLine("#nullable enable");
        writer.WriteLine("#pragma warning disable CS9113 // Parameter is unread.");
        writer.WriteLine();
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine("using System.Linq;");
        writer.WriteLine("using System.Reflection;");
        writer.WriteLine("using global::TUnit.Core;");
        writer.WriteLine("using global::TUnit.Core.SourceGenerator;");
        writer.WriteLine();
        writer.WriteLine("namespace TUnit.Generated;");
        writer.WriteLine();
        writer.WriteLine($"internal static class {registryClassName}");
        writer.WriteLine("{");
        writer.WriteLine("[System.Runtime.CompilerServices.ModuleInitializer]");
        writer.WriteLine("public static void Initialize()");
        writer.WriteLine("{");
        writer.WriteLine("var testMetadata = new System.Collections.Generic.List<DynamicTestMetadata>();");
        writer.WriteLine();
        
        // Generate test metadata
        writer.WriteLine("testMetadata.Add(new DynamicTestMetadata");
        writer.WriteLine("{");
        writer.WriteLine($"TestIdTemplate = \"{GetFullTypeName(classSymbol)}.{methodSymbol.Name}_{{{{TestIndex}}}}\",");
        writer.WriteLine($"TestClassTypeReference = {CodeGenerationHelpers.GenerateTypeReference(classSymbol)},");
        writer.WriteLine($"TestClassType = typeof({GetFullTypeName(classSymbol)}),");
        
        // Generate TestClassFactory
        writer.Write("TestClassFactory = ");
        writer.Write(GenerateTestClassFactory(classSymbol));
        writer.WriteLine(",");
        
        // Generate MethodMetadata
        writer.WriteLine("MethodMetadata = new MethodMetadata");
        writer.WriteLine("{");
        writer.WriteLine($"Name = \"{methodSymbol.Name}\",");
        writer.WriteLine($"Type = typeof({GetFullTypeName(baseClass)}) ?? typeof(object),");
        writer.WriteLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(baseClass)},");
        writer.WriteLine($"Parameters = {CodeGenerationHelpers.GenerateParameterMetadataArray(methodSymbol)},");
        writer.WriteLine($"GenericTypeCount = {methodSymbol.TypeParameters.Length},");
        writer.WriteLine($"ReturnTypeReference = {CodeGenerationHelpers.GenerateTypeReference(methodSymbol.ReturnType)},");
        writer.WriteLine($"ReturnType = {(ContainsTypeParameter(methodSymbol.ReturnType) ? "null" : $"typeof({GetReturnTypeName(methodSymbol)})!")},");
        
        // Generate ClassMetadata
        writer.WriteLine("Class = new ClassMetadata");
        writer.WriteLine("{");
        writer.WriteLine($"Name = \"{classSymbol.Name}\",");
        writer.WriteLine($"Type = typeof({GetFullTypeName(classSymbol)}) ?? typeof(object),");
        writer.WriteLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(classSymbol)},");
        writer.WriteLine($"Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(classSymbol.GetAttributes())},");
        writer.WriteLine($"Namespace = \"{classSymbol.ContainingNamespace?.ToDisplayString() ?? ""}\",");
        
        // Generate Assembly metadata
        writer.WriteLine("Assembly = new AssemblyMetadata");
        writer.WriteLine("{");
        writer.WriteLine($"Name = \"{classSymbol.ContainingAssembly.Name}\",");
        writer.WriteLine($"Attributes = System.Array.Empty<global::TUnit.Core.AttributeMetadata>()");
        writer.WriteLine("},");
        
        // Generate Parameters (for class constructor)
        writer.WriteLine($"Parameters = System.Array.Empty<global::TUnit.Core.ParameterMetadata>(),");
        writer.WriteLine($"Properties = {CodeGenerationHelpers.GeneratePropertyMetadataArray(classSymbol)},");
        writer.WriteLine($"Constructors = {CodeGenerationHelpers.GenerateConstructorMetadataArray(classSymbol)},");
        writer.WriteLine("Parent = null");
        writer.WriteLine("},");
        writer.WriteLine($"Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(methodSymbol.GetAttributes())},");
        writer.WriteLine($"ReflectionInformation = typeof({GetFullTypeName(baseClass)}).GetMethod(\"{methodSymbol.Name}\", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)");
        writer.WriteLine("},");
        
        writer.WriteLine($"DisplayNameTemplate = \"{GetDisplayName(classSymbol, methodSymbol, testMethodInfo.TestAttribute)}\",");
        writer.WriteLine($"TestFilePath = \"{classInfo.FilePath.Replace("\\", "\\\\")}\",");
        writer.WriteLine($"TestLineNumber = {classInfo.LineNumber},");
        writer.WriteLine($"ClassDataSources = {CodeGenerationHelpers.GenerateClassDataSourceProviders(classSymbol)},");
        writer.WriteLine($"MethodDataSources = {CodeGenerationHelpers.GenerateMethodDataSourceProviders(methodSymbol)},");
        writer.WriteLine($"PropertyDataSources = {CodeGenerationHelpers.GeneratePropertyDataSourceDictionary(classSymbol)},");
        writer.WriteLine($"Attributes = {CodeGenerationHelpers.GenerateTestAttributes(methodSymbol)},");
        
        // Extract timeout, skip info, and repeat count
        var (isSkipped, skipReason) = CodeGenerationHelpers.ExtractSkipInfo(methodSymbol);
        writer.WriteLine($"IsSkipped = {(isSkipped ? "true" : "false")},");
        writer.WriteLine($"SkipReason = {skipReason},");
        writer.WriteLine($"Timeout = {CodeGenerationHelpers.ExtractTimeout(methodSymbol)},");
        writer.WriteLine($"RepeatCount = {CodeGenerationHelpers.ExtractRepeatCount(methodSymbol)},");
        writer.WriteLine($"IsAsync = {(IsAsyncMethod(methodSymbol) ? "true" : "false")}");
        
        writer.WriteLine("});");
        writer.WriteLine();
        writer.WriteLine("TestSourceRegistrar.RegisterTests(testMetadata.Cast<ITestDescriptor>().ToList());");
        writer.WriteLine("}");
        writer.WriteLine("}");

        return writer.ToString();
    }

    private static string GetFullTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
    }

    private static string GenerateTestClassFactory(INamedTypeSymbol classSymbol)
    {
        // For generic types, we can't create instances at compile time
        if (classSymbol.IsGenericType)
        {
            return "null!"; // Will be replaced at runtime by TestBuilder
        }
        
        using var writer = new SourceCodeWriter(1); // Start with indent for inline expression
        writer.Write("args => ");
        
        // Find appropriate constructor
        var constructors = classSymbol.Constructors
            .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
            .OrderBy(c => c.Parameters.Length)
            .ToList();
        
        var hasParameterlessConstructor = constructors.Any(c => c.Parameters.Length == 0);
        var constructorWithParameters = !hasParameterlessConstructor ? constructors.FirstOrDefault() : null;
        
        // Check for required properties that don't have data source attributes in this class and all base classes
        var requiredProperties = GetAllRequiredPropertiesWithoutDataSource(classSymbol);
        
        var className = GetFullTypeName(classSymbol);
        
        // If the class has a constructor with parameters and no parameterless constructor
        if (constructorWithParameters != null && !hasParameterlessConstructor)
        {
            // Use the args parameter which contains class constructor arguments
            writer.Write($"new {className}(");
            
            // Generate argument list with proper type casting
            var parameterList = string.Join(", ", constructorWithParameters.Parameters
                .Select((param, i) => 
                {
                    var typeName = GetFullTypeName(param.Type);
                    return $"({typeName})args[{i}]";
                }));
            
            writer.Write(parameterList);
            writer.Write(")");
            
            // If there are also required properties, add object initializer
            if (requiredProperties.Any())
            {
                writer.Write(" { ");
                var propertyInitializers = requiredProperties.Select(prop => 
                {
                    var defaultValue = GetDefaultValueForType(prop.Type);
                    return $"{prop.Name} = {defaultValue}";
                });
                writer.Write(string.Join(", ", propertyInitializers));
                writer.Write(" }");
            }
        }
        else if (requiredProperties.Any())
        {
            // Only required properties, no constructor parameters
            writer.Write($"new {className} {{ ");
            
            var propertyInitializers = requiredProperties.Select(prop => 
            {
                var defaultValue = GetDefaultValueForType(prop.Type);
                return $"{prop.Name} = {defaultValue}";
            });
            
            writer.Write(string.Join(", ", propertyInitializers));
            writer.Write(" }");
        }
        else
        {
            // Simple parameterless constructor
            writer.Write($"new {className}()");
        }
        
        return writer.ToString().Trim(); // Trim to remove any extra newlines
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

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
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