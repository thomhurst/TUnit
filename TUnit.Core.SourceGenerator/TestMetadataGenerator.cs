using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
        if (typeSymbol.IsAbstract || methodSymbol.IsStatic || (typeSymbol.IsGenericType && typeSymbol.TypeParameters.Length > 0))
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

        using var writer = new SourceCodeWriter();
        
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

        writer.WriteLine("#nullable enable");
        writer.WriteLine("#pragma warning disable CS9113 // Parameter is unread.");
        writer.WriteLine();
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine("using System.Linq;");
        writer.WriteLine("using System.Reflection;");
        writer.WriteLine("using global::TUnit.Core;");
        writer.WriteLine();
        writer.WriteLine("using global::TUnit.Core.SourceGenerator;");
        writer.WriteLine();
        writer.WriteLine("namespace TUnit.Generated;");
        writer.WriteLine();
        writer.WriteLine($"internal static class TestMetadataRegistry_{safeClassName}_{safeMethodName}_{guid}");
        writer.WriteLine("{");
        writer.WriteLine("[System.Runtime.CompilerServices.ModuleInitializer]");
        writer.WriteLine("public static void Initialize()");
        writer.WriteLine("{");
        writer.WriteLine("var testMetadata = new System.Collections.Generic.List<DynamicTestMetadata>();");
        writer.WriteLine();
        // Extract skip information
        var (isSkipped, skipReason) = CodeGenerationHelpers.ExtractSkipInfo(testInfo.MethodSymbol);
        
        // For generic types, we can't use typeof() so TestClassType will be null
        var testClassTypeValue = testInfo.TypeSymbol.IsGenericType ? "null" : $"typeof({className})";
        
        writer.WriteLine("testMetadata.Add(new DynamicTestMetadata");
        writer.WriteLine("{");
        writer.WriteLine($"TestIdTemplate = \"{className}.{methodName}_{{{{TestIndex}}}}\",");
        writer.WriteLine($"TestClassTypeReference = {CodeGenerationHelpers.GenerateTypeReference(testInfo.TypeSymbol)},");
        writer.WriteLine($"TestClassType = {testClassTypeValue},");
        writer.WriteLine("MethodMetadata = new MethodMetadata");
        writer.WriteLine("{");
        writer.WriteLine($"Name = \"{testInfo.MethodSymbol.Name}\",");
        writer.WriteLine($"Type = {testClassTypeValue} ?? typeof(object),");
        writer.WriteLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(testInfo.TypeSymbol)},");
        writer.WriteLine($"Parameters = {CodeGenerationHelpers.GenerateParameterMetadataArray(testInfo.MethodSymbol)},");
        writer.WriteLine($"GenericTypeCount = {testInfo.MethodSymbol.TypeParameters.Length},");
        writer.WriteLine("Class = new ClassMetadata");
        writer.WriteLine("{");
        writer.WriteLine($"Name = \"{testInfo.TypeSymbol.Name}\",");
        writer.WriteLine($"Type = {testClassTypeValue} ?? typeof(object),");
        writer.WriteLine($"TypeReference = {CodeGenerationHelpers.GenerateTypeReference(testInfo.TypeSymbol)},");
        writer.WriteLine($"Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(testInfo.TypeSymbol.GetAttributes())},");
        writer.WriteLine($"Namespace = \"{testInfo.TypeSymbol.ContainingNamespace}\",");
        writer.WriteLine($"Assembly = new AssemblyMetadata {{ Name = \"{testInfo.TypeSymbol.ContainingAssembly.Name}\", Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(testInfo.TypeSymbol.ContainingAssembly.GetAttributes())} }},");
        writer.WriteLine("Parameters = System.Array.Empty<ParameterMetadata>(),");
        writer.WriteLine($"Properties = {CodeGenerationHelpers.GeneratePropertyMetadataArray(testInfo.TypeSymbol)},");
        writer.WriteLine($"Constructors = {CodeGenerationHelpers.GenerateConstructorMetadataArray(testInfo.TypeSymbol)},");
        writer.WriteLine("Parent = null");
        writer.WriteLine("},");
        writer.WriteLine($"ReturnType = {(ContainsTypeParameter(testInfo.MethodSymbol.ReturnType) ? "null" : $"typeof({GetReturnTypeName(testInfo.MethodSymbol)})")},");
        writer.WriteLine($"ReturnTypeReference = {CodeGenerationHelpers.GenerateTypeReference(testInfo.MethodSymbol.ReturnType)},");
        writer.WriteLine($"Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(testInfo.MethodSymbol.GetAttributes())}");
        writer.WriteLine("},");
        writer.WriteLine($"TestFilePath = @\"{testInfo.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
        writer.WriteLine($"TestLineNumber = {testInfo.LineNumber},");
        writer.WriteLine($"TestClassFactory = {GenerateTestClassFactory(testInfo.TypeSymbol, className, requiredProperties, constructorWithParameters, hasParameterlessConstructor)},");
        writer.WriteLine($"ClassDataSources = {CodeGenerationHelpers.GenerateClassDataSourceProviders(testInfo.TypeSymbol)},");
        writer.WriteLine($"MethodDataSources = {CodeGenerationHelpers.GenerateMethodDataSourceProviders(testInfo.MethodSymbol)},");
        writer.WriteLine($"PropertyDataSources = {CodeGenerationHelpers.GeneratePropertyDataSourceDictionary(testInfo.TypeSymbol)},");
        writer.WriteLine($"DisplayNameTemplate = \"{methodName}\",");
        writer.WriteLine($"RepeatCount = {CodeGenerationHelpers.ExtractRepeatCount(testInfo.MethodSymbol)},");
        writer.WriteLine($"IsAsync = {(IsAsyncMethod(testInfo.MethodSymbol) ? "true" : "false")},");
        writer.WriteLine($"IsSkipped = {(isSkipped ? "true" : "false")},");
        writer.WriteLine($"SkipReason = {skipReason},");
        writer.WriteLine($"Attributes = {CodeGenerationHelpers.GenerateTestAttributes(testInfo.MethodSymbol)},");
        writer.WriteLine($"Timeout = {CodeGenerationHelpers.ExtractTimeout(testInfo.MethodSymbol)}");
        writer.WriteLine("});");
        writer.WriteLine();
        writer.WriteLine("TestSourceRegistrar.RegisterTests(testMetadata.Cast<ITestDescriptor>().ToList());");
        writer.WriteLine("}");
        writer.WriteLine("}");

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
            .Concat(new[] { '<', '>', '(', ')', '[', ']', '{', '}', ',', ' ', '`', '.' })
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
        
        using var writer = new SourceCodeWriter(1); // Start with indent for inline expression
        writer.Write("args => ");
        
        // If the class has a constructor with parameters and no parameterless constructor
        if (constructorWithParameters != null && !hasParameterlessConstructor)
        {
            // Use the args parameter which contains class constructor arguments
            writer.Write($"new {className}(");
            
            // Generate argument list with proper type casting
            var parameterList = string.Join(", ", constructorWithParameters.Parameters
                .Select((param, i) => 
                {
                    var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
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