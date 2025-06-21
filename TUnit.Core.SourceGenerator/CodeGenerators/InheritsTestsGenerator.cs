using System.Collections.Immutable;
using System.Reflection;
using System.Text;
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
        var sb = new StringBuilder();
        var classSymbol = classInfo.ClassSymbol;
        var methodSymbol = testMethodInfo.Method;
        var baseClass = testMethodInfo.BaseClass;
        
        // Generate unique registry class name
        var registryClassName = $"TestMetadataRegistry_{classSymbol.Name}_{methodSymbol.Name}_{Guid.NewGuid():N}";
        
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable CS9113 // Parameter is unread.");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Reflection;");
        sb.AppendLine("using global::TUnit.Core;");
        sb.AppendLine("using global::TUnit.Core.SourceGenerator;");
        sb.AppendLine();
        sb.AppendLine("namespace TUnit.Generated;");
        sb.AppendLine();
        sb.AppendLine($"internal static class {registryClassName}");
        sb.AppendLine("{");
        sb.AppendLine("    [System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    public static void Initialize()");
        sb.AppendLine("    {");
        sb.AppendLine("        var testMetadata = new System.Collections.Generic.List<TestMetadata>();");
        sb.AppendLine();
        
        // Generate test metadata
        sb.AppendLine("        testMetadata.Add(new TestMetadata");
        sb.AppendLine("        {");
        sb.AppendLine($"            TestIdTemplate = \"{GetFullTypeName(classSymbol)}.{methodSymbol.Name}_{{{{TestIndex}}}}\",");
        sb.AppendLine($"            TestClassTypeReference = {CodeGenerationHelpers.GenerateTypeReference(classSymbol)},");
        sb.AppendLine($"            TestClassType = typeof({GetFullTypeName(classSymbol)}),");
        
        // Generate TestClassFactory
        sb.Append("            TestClassFactory = ");
        sb.AppendLine(GenerateTestClassFactory(classSymbol));
        sb.AppendLine(",");
        
        // Generate MethodMetadata
        sb.AppendLine("            MethodMetadata = new MethodMetadata");
        sb.AppendLine("            {");
        sb.AppendLine($"                Name = \"{methodSymbol.Name}\",");
        sb.AppendLine($"                Type = typeof({GetFullTypeName(baseClass)}) ?? typeof(object),");
        sb.AppendLine($"                TypeReference = {CodeGenerationHelpers.GenerateTypeReference(baseClass)},");
        sb.AppendLine($"                Parameters = {CodeGenerationHelpers.GenerateParameterMetadataArray(methodSymbol)},");
        sb.AppendLine($"                GenericTypeCount = {methodSymbol.TypeParameters.Length},");
        sb.AppendLine($"                ReturnTypeReference = {CodeGenerationHelpers.GenerateTypeReference(methodSymbol.ReturnType)},");
        sb.AppendLine($"                ReturnType = {(ContainsTypeParameter(methodSymbol.ReturnType) ? "null" : $"typeof({GetReturnTypeName(methodSymbol)})!")},");
        
        // Generate ClassMetadata
        sb.AppendLine("                Class = new ClassMetadata");
        sb.AppendLine("                {");
        sb.AppendLine($"                    Name = \"{classSymbol.Name}\",");
        sb.AppendLine($"                    Type = typeof({GetFullTypeName(classSymbol)}) ?? typeof(object),");
        sb.AppendLine($"                    TypeReference = {CodeGenerationHelpers.GenerateTypeReference(classSymbol)},");
        sb.AppendLine($"                    Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(classSymbol.GetAttributes())},");
        sb.AppendLine($"                    Namespace = \"{classSymbol.ContainingNamespace?.ToDisplayString() ?? ""}\",");
        
        // Generate Assembly metadata
        sb.AppendLine("                    Assembly = new AssemblyMetadata");
        sb.AppendLine("                    {");
        sb.AppendLine($"                        Name = \"{classSymbol.ContainingAssembly.Name}\",");
        sb.AppendLine($"                        Attributes = System.Array.Empty<global::TUnit.Core.AttributeMetadata>()");
        sb.AppendLine("                    },");
        
        // Generate Parameters (for class constructor)
        sb.AppendLine($"                    Parameters = System.Array.Empty<global::TUnit.Core.ParameterMetadata>(),");
        sb.AppendLine($"                    Properties = {CodeGenerationHelpers.GeneratePropertyMetadataArray(classSymbol)},");
        sb.AppendLine($"                    Constructors = {CodeGenerationHelpers.GenerateConstructorMetadataArray(classSymbol)},");
        sb.AppendLine("                    Parent = null");
        sb.AppendLine("                },");
        sb.AppendLine($"                Attributes = {CodeGenerationHelpers.GenerateAttributeMetadataArray(methodSymbol.GetAttributes())},");
        sb.AppendLine($"                ReflectionInformation = typeof({GetFullTypeName(baseClass)}).GetMethod(\"{methodSymbol.Name}\", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)");
        sb.AppendLine("            },");
        
        sb.AppendLine($"            DisplayNameTemplate = \"{GetDisplayName(classSymbol, methodSymbol, testMethodInfo.TestAttribute)}\",");
        sb.AppendLine($"            TestFilePath = \"{classInfo.FilePath.Replace("\\", "\\\\")}\",");
        sb.AppendLine($"            TestLineNumber = {classInfo.LineNumber},");
        sb.AppendLine($"            ClassDataSources = {CodeGenerationHelpers.GenerateClassDataSourceProviders(classSymbol)},");
        sb.AppendLine($"            MethodDataSources = {CodeGenerationHelpers.GenerateMethodDataSourceProviders(methodSymbol)},");
        sb.AppendLine($"            PropertyDataSources = {CodeGenerationHelpers.GeneratePropertyDataSourceDictionary(classSymbol)},");
        sb.AppendLine($"            Attributes = {CodeGenerationHelpers.GenerateTestAttributes(methodSymbol)},");
        
        // Extract timeout, skip info, and repeat count
        var (isSkipped, skipReason) = CodeGenerationHelpers.ExtractSkipInfo(methodSymbol);
        sb.AppendLine($"            IsSkipped = {(isSkipped ? "true" : "false")},");
        sb.AppendLine($"            SkipReason = {skipReason},");
        sb.AppendLine($"            Timeout = {CodeGenerationHelpers.ExtractTimeout(methodSymbol)},");
        sb.AppendLine($"            RepeatCount = {CodeGenerationHelpers.ExtractRepeatCount(methodSymbol)},");
        sb.AppendLine($"            IsAsync = {(IsAsyncMethod(methodSymbol) ? "true" : "false")}");
        
        sb.AppendLine("        });");
        sb.AppendLine();
        sb.AppendLine("        TestSourceRegistrar.RegisterMetadata(testMetadata);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
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
        
        var sb = new StringBuilder();
        sb.Append("args => ");
        
        // Find appropriate constructor
        var constructors = classSymbol.Constructors
            .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
            .OrderBy(c => c.Parameters.Length)
            .ToList();
        
        var hasParameterlessConstructor = constructors.Any(c => c.Parameters.Length == 0);
        var constructorWithParameters = !hasParameterlessConstructor ? constructors.FirstOrDefault() : null;
        
        // Check for required properties
        var requiredProperties = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.IsRequired)
            .ToList();
        
        var className = GetFullTypeName(classSymbol);
        
        // If the class has a constructor with parameters and no parameterless constructor
        if (constructorWithParameters != null && !hasParameterlessConstructor)
        {
            // Use the args parameter which contains class constructor arguments
            sb.Append($"new {className}(");
            
            // Generate argument list with proper type casting
            var parameterList = string.Join(", ", constructorWithParameters.Parameters
                .Select((param, i) => 
                {
                    var typeName = GetFullTypeName(param.Type);
                    return $"({typeName})args[{i}]";
                }));
            
            sb.Append(parameterList);
            sb.Append(")");
            
            // If there are also required properties, add object initializer
            if (requiredProperties.Any())
            {
                sb.Append(" { ");
                var propertyInitializers = requiredProperties.Select(prop => 
                {
                    var defaultValue = GetDefaultValueForType(prop.Type);
                    return $"{prop.Name} = {defaultValue}";
                });
                sb.Append(string.Join(", ", propertyInitializers));
                sb.Append(" }");
            }
        }
        else if (requiredProperties.Any())
        {
            // Only required properties, no constructor parameters
            sb.Append($"new {className} {{ ");
            
            var propertyInitializers = requiredProperties.Select(prop => 
            {
                var defaultValue = GetDefaultValueForType(prop.Type);
                return $"{prop.Name} = {defaultValue}";
            });
            
            sb.Append(string.Join(", ", propertyInitializers));
            sb.Append(" }");
        }
        else
        {
            // Simple parameterless constructor
            sb.Append($"new {className}()");
        }
        
        return sb.ToString();
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