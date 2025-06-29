using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Models;
using TUnit.Core.SourceGenerator.Helpers;

namespace TUnit.Core.SourceGenerator;

/// <summary>
/// Simplified source generator that emits unified TestMetadata with AOT support
/// </summary>
[Generator]
public class UnifiedTestMetadataGenerator : IIncrementalGenerator
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

        // Collect all test methods to generate a single registry
        var collected = testMethods.Collect();
        
        // Generate the test registry
        context.RegisterSourceOutput(collected, GenerateTestRegistry);
    }

    private static TestMethodMetadata? GetTestMethodMetadata(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        var typeSymbol = methodSymbol.ContainingType;
        if (typeSymbol == null || typeSymbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return null;
        }

        // Skip abstract classes and static methods
        if (namedTypeSymbol.IsAbstract || methodSymbol.IsStatic)
        {
            return null;
        }
        
        // Skip generic type definitions with unresolved type parameters
        // These will be handled through InheritsTests or explicit instantiation
        if (namedTypeSymbol.IsGenericType && namedTypeSymbol.TypeArguments.Any(t => t is ITypeParameterSymbol))
        {
            return null;
        }

        // Skip non-public methods
        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }
        
        // Skip tests on base classes if derived class has InheritsTests attribute
        // This prevents duplicate test registration
        if (IsTestInheritedByDerivedClass(methodSymbol, namedTypeSymbol))
        {
            return null;
        }
        
        // Skip tests with AsyncDataSourceGenerator attributes - they're handled by specialized generators
        var hasAsyncDataSourceGenerator = methodSymbol.GetAttributes()
            .Any(a => a.AttributeClass != null && IsAsyncDataSourceGeneratorAttribute(a.AttributeClass));
            
        if (hasAsyncDataSourceGenerator)
        {
            return null;
        }

        // Get location info
        var location = context.TargetNode.GetLocation();
        var filePath = location.SourceTree?.FilePath ?? "";
        var lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;

        // Make sure we have at least one attribute (the Test attribute)
        if (context.Attributes.Length == 0)
        {
            return null;
        }

        return new TestMethodMetadata
        {
            MethodSymbol = methodSymbol,
            TypeSymbol = namedTypeSymbol,
            FilePath = filePath,
            LineNumber = lineNumber,
            TestAttribute = context.Attributes[0],
            Context = context
        };
    }
    
    private static bool IsAsyncDataSourceGeneratorAttribute(INamedTypeSymbol attributeClass)
    {
        // Check if the attribute inherits from AsyncDataSourceGeneratorAttribute
        var baseType = attributeClass.BaseType;
        while (baseType != null)
        {
            if (baseType.Name.StartsWith("AsyncDataSourceGeneratorAttribute") && 
                baseType.ContainingNamespace?.ToString() == "TUnit.Core")
            {
                return true;
            }
            
            // Also check interfaces
            if (baseType.Interfaces.Any(i => i.Name == "IAsyncDataSourceGeneratorAttribute" && 
                i.ContainingNamespace?.ToString() == "TUnit.Core"))
            {
                return true;
            }
            
            baseType = baseType.BaseType;
        }
        
        return false;
    }

    private static void GenerateTestRegistry(SourceProductionContext context, ImmutableArray<TestMethodMetadata?> testMethods)
    {
        try
        {
            var validTests = testMethods.Where(t => t != null).Cast<TestMethodMetadata>().ToList();
            if (!validTests.Any())
            {
                return;
            }

            using var writer = new CodeWriter();
        
        // Write file header
        writer.AppendLine("#nullable enable");
        writer.AppendLine("#pragma warning disable CS9113 // Parameter is unread.");
        writer.AppendLine();
        writer.AppendLine("using System;");
        writer.AppendLine("using System.Collections.Generic;");
        writer.AppendLine("using System.Linq;");
        writer.AppendLine("using System.Threading.Tasks;");
        writer.AppendLine("using global::TUnit.Core;");
        writer.AppendLine("using global::TUnit.Engine;");
        writer.AppendLine();
        
        writer.AppendLine("namespace TUnit.Generated;");
        writer.AppendLine();
        
        // Generate the registry class
        using (writer.BeginBlock("public static class UnifiedTestMetadataRegistry"))
        {
            // Generate static test metadata collection
            writer.AppendLine("private static readonly List<TestMetadata> _allTests = new();");
            writer.AppendLine();
            
            // Module initializer to register all tests
            writer.AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]");
            using (writer.BeginBlock("public static void Initialize()"))
            {
                writer.AppendLine("try");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("RegisterAllTests();");
                writer.AppendLine();
                writer.AppendLine("// Register with the discovery service");
                writer.AppendLine("var source = new global::TUnit.Engine.SourceGeneratedTestMetadataSource(() => _allTests);");
                writer.AppendLine("global::TUnit.Engine.TestMetadataRegistry.RegisterSource(source);");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine("catch (Exception ex)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("Console.Error.WriteLine($\"Failed to initialize test registry: {ex}\");");
                writer.AppendLine("throw;");
                writer.Unindent();
                writer.AppendLine("}");
            }
            
            writer.AppendLine();
            
            // Generate the registration method
            using (writer.BeginBlock("private static void RegisterAllTests()"))
            {
                writer.AppendLine($"// Registering {validTests.Count} tests");
                writer.AppendLine($"Console.Error.WriteLine(\"Registering {validTests.Count} tests...\");");
                writer.AppendLine();
                
                foreach (var testInfo in validTests)
                {
                    if (testInfo.MethodSymbol.IsGenericMethod)
                    {
                        GenerateGenericTestMetadata(writer, testInfo);
                    }
                    else
                    {
                        GenerateTestMetadata(writer, testInfo);
                    }
                    writer.AppendLine();
                }
                
                writer.AppendLine($"Console.Error.WriteLine(\"All {validTests.Count} tests registered successfully\");");
            }
            
            writer.AppendLine();
            
            // Generate helper methods for each test class
            var testClasses = validTests.GroupBy(t => t.TypeSymbol, SymbolEqualityComparer.Default);
            foreach (var classGroup in testClasses)
            {
                if (classGroup.Key is INamedTypeSymbol namedType)
                {
                    GenerateTestClassHelpers(writer, namedType, classGroup.ToList());
                }
            }
        }
        
            context.AddSource("UnifiedTestMetadataRegistry.g.cs", writer.ToString());
        }
        catch (Exception ex)
        {
            // Generate diagnostic error code that will help identify the issue
            var descriptor = new DiagnosticDescriptor(
                "TUG001",
                "Test metadata generation failed",
                "Failed to generate test metadata: {0}",
                "TUnit",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
            
            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, ex.ToString()));
        }
    }
    
    private static void GenerateTestMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        TestMetadataGenerationContext context;
        try
        {
            context = TestMetadataGenerationContext.Create(testInfo);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create context for {testInfo?.TypeSymbol?.Name}.{testInfo?.MethodSymbol?.Name}: {ex.Message}", ex);
        }
        
        var paramTypeNames = testInfo.MethodSymbol.Parameters
            .Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))
            .ToArray();
        var testId = paramTypeNames.Length > 0 
            ? $"{context.ClassName}.{context.MethodName}({string.Join(",", paramTypeNames)})"
            : $"{context.ClassName}.{context.MethodName}";
        
        using (writer.BeginBlock("_allTests.Add(new TestMetadata"))
        {
            writer.AppendLine($"TestId = \"{testId}\",");
            writer.AppendLine($"TestName = \"{context.MethodName}\",");
            
            writer.AppendLine($"TestClassType = typeof({context.ClassName}),");
            
            writer.AppendLine($"TestMethodName = \"{context.MethodName}\",");
            
            try
            {
                // Categories
                GenerateCategories(writer, testInfo);
                
                // Skip status
                GenerateSkipStatus(writer, testInfo);
                
                // Timeout
                GenerateTimeout(writer, testInfo);
                
                // Retry count
                writer.AppendLine($"RetryCount = {GetRetryCount(testInfo)},");
                
                // Parallelization
                writer.AppendLine($"CanRunInParallel = {GetCanRunInParallel(testInfo).ToString().ToLower()},");
                
                // Dependencies
                //GenerateDependencies(writer, testInfo);
                writer.AppendLine("DependsOn = Array.Empty<string>(),");
                
                // Data sources
                GenerateDataSources(writer, testInfo);
                
                // Property data sources
                GeneratePropertyDataSources(writer, testInfo);
                
                // Parameter info
                writer.AppendLine($"ParameterCount = {testInfo.MethodSymbol.Parameters.Length},");
                GenerateParameterTypes(writer, testInfo);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate metadata fields for {context.ClassName}.{context.MethodName}: {ex.Message}", ex);
            }
            
            // AOT factories (if possible)
            if (context.CanUseStaticDefinition && context.HasParameterlessConstructor && 
                !testInfo.MethodSymbol.IsGenericMethod && !testInfo.TypeSymbol.IsGenericType)
            {
                writer.AppendLine($"InstanceFactory = () => new {context.ClassName}(),");
                writer.AppendLine($"TestInvoker = {context.SafeClassName}_{context.SafeMethodName}_Invoker,");
            }
            else
            {
                writer.AppendLine("InstanceFactory = null,");
                writer.AppendLine("TestInvoker = null,");
            }
            
            // Hooks
            //GenerateHooks(writer, testInfo);
            writer.AppendLine("Hooks = new TestHooks { BeforeClass = Array.Empty<HookMetadata>(), AfterClass = Array.Empty<HookMetadata>(), BeforeTest = Array.Empty<HookMetadata>(), AfterTest = Array.Empty<HookMetadata>() },");
            
            // Always provide MethodInfo for reflection fallback
            if (testInfo.MethodSymbol.IsGenericMethod || testInfo.TypeSymbol.IsGenericType)
            {
                // For generic methods or types, we need to get the method without specifying parameter types
                // because they may contain generic type parameters
                var typeForMethodLookup = testInfo.TypeSymbol.IsGenericType
                    ? testInfo.TypeSymbol.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))
                    : context.ClassName;
                writer.AppendLine($"MethodInfo = typeof({typeForMethodLookup}).GetMethod(\"{context.MethodName}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance),");
            }
            else if (testInfo.MethodSymbol.Parameters.Length == 0)
            {
                writer.AppendLine($"MethodInfo = typeof({context.ClassName}).GetMethod(\"{context.MethodName}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null),");
            }
            else
            {
                writer.Append($"MethodInfo = typeof({context.ClassName}).GetMethod(\"{context.MethodName}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, new Type[] {{ ");
                var parameterTypes = testInfo.MethodSymbol.Parameters.Select(p =>
                {
                    var typeName = p.Type.ToDisplayString();
                    // Remove nullable annotations for typeof()
                    typeName = typeName.Replace("?", "");
                    return $"typeof({typeName})";
                });
                writer.Append(string.Join(", ", parameterTypes));
                writer.AppendLine(" }, null),");
            }
            
            // Source location
            writer.AppendLine($"FilePath = @\"{testInfo.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
            writer.AppendLine($"LineNumber = {testInfo.LineNumber},");
            
            // Generic type information
            GenerateGenericTypeInfo(writer, testInfo);
            
            // Generic method information
            GenerateGenericMethodInfo(writer, testInfo);
        }
        writer.AppendLine(");");
    }
    
    private static void GenerateGenericTestMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // For generic methods, we generate metadata that will be resolved at runtime
        // We do NOT try to resolve generic types at compile time
        GenerateTestMetadata(writer, testInfo);
    }
    
    
    
    private static void GenerateStaticDataSourceForArguments(CodeWriter writer, AttributeData argumentsAttribute)
    {
        writer.Append("new StaticTestDataSource(new object?[] { ");
        
        var args = new List<string>();
        foreach (var arg in argumentsAttribute.ConstructorArguments)
        {
            if (arg.Kind == TypedConstantKind.Array)
            {
                // Handle params array
                foreach (var item in arg.Values)
                {
                    args.Add(FormatAttributeArgument(item.Value));
                }
            }
            else
            {
                args.Add(FormatAttributeArgument(arg.Value));
            }
        }
        
        writer.Append(string.Join(", ", args));
        writer.AppendLine(" })");
    }
    
    private static ITypeSymbol[]? ExtractArgumentTypes(AttributeData attributeData)
    {
        var types = new List<ITypeSymbol>();
        
        foreach (var arg in attributeData.ConstructorArguments)
        {
            if (arg.Kind == TypedConstantKind.Array)
            {
                // Handle params array
                foreach (var item in arg.Values)
                {
                    var type = GetTypeFromValue(item);
                    if (type != null)
                    {
                        types.Add(type);
                    }
                }
            }
            else
            {
                var type = GetTypeFromValue(arg);
                if (type != null)
                {
                    types.Add(type);
                }
            }
        }
        
        return types.ToArray();
    }
    
    private static ITypeSymbol? GetTypeFromValue(TypedConstant constant)
    {
        if (constant.Type == null)
        {
            return null;
        }
        
        // For typed constants, the Type property gives us the actual type
        return constant.Type;
    }
    
    private static void GenerateTestClassHelpers(CodeWriter writer, INamedTypeSymbol classSymbol, List<TestMethodMetadata> testMethods)
    {
        var className = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        var safeClassName = className.Replace(".", "_").Replace("<", "_").Replace(">", "_");
        
        writer.AppendLine($"#region Helpers for {className}");
        writer.AppendLine();
        
        // Track generated method names to avoid duplicates
        var generatedMethods = new HashSet<string>();
        
        // Generate test invokers for AOT
        foreach (var testInfo in testMethods)
        {
            var context = TestMetadataGenerationContext.Create(testInfo);
            if (context.CanUseStaticDefinition && context.HasParameterlessConstructor)
            {
                var methodName = $"{context.SafeClassName}_{context.SafeMethodName}_Invoker";
                if (!generatedMethods.Contains(methodName))
                {
                    generatedMethods.Add(methodName);
                    GenerateTestInvoker(writer, testInfo, context);
                }
            }
        }
        
        // Generate hook invokers
        GenerateHookInvokers(writer, classSymbol);
        
        writer.AppendLine($"#endregion");
        writer.AppendLine();
    }
    
    private static void GenerateTestInvoker(CodeWriter writer, TestMethodMetadata testInfo, TestMetadataGenerationContext context)
    {
        // Skip generating invokers for generic methods - they'll use reflection
        if (testInfo.MethodSymbol.IsGenericMethod || testInfo.TypeSymbol.IsGenericType)
        {
            return;
        }
        
        var methodName = $"{context.SafeClassName}_{context.SafeMethodName}_Invoker";
        
        using (writer.BeginBlock($"private static async Task {methodName}(object instance, object?[] args)"))
        {
            writer.AppendLine($"var typedInstance = ({context.ClassName})instance;");
            
            // Generate parameter extraction
            for (int i = 0; i < testInfo.MethodSymbol.Parameters.Length; i++)
            {
                var param = testInfo.MethodSymbol.Parameters[i];
                var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                // Handle nullable reference types in casts
                writer.AppendLine($"var arg{i} = ({paramType})args[{i}]!;");
            }
            
            // Generate method call
            var argList = string.Join(", ", Enumerable.Range(0, testInfo.MethodSymbol.Parameters.Length).Select(i => $"arg{i}"));
            var methodCall = $"typedInstance.{context.MethodName}({argList})";
            
            if (IsAsyncMethod(testInfo.MethodSymbol))
            {
                writer.AppendLine($"await {methodCall};");
            }
            else
            {
                writer.AppendLine($"{methodCall};");
            }
        }
        writer.AppendLine();
    }
    
    private static void GenerateHookInvokers(CodeWriter writer, INamedTypeSymbol classSymbol)
    {
        // Find all hook methods in the class hierarchy
        var beforeClassHooks = FindHooksInHierarchy(classSymbol, "BeforeAttribute", isStatic: true);
        var afterClassHooks = FindHooksInHierarchy(classSymbol, "AfterAttribute", isStatic: true);
        var beforeTestHooks = FindHooksInHierarchy(classSymbol, "BeforeAttribute", isStatic: false);
        var afterTestHooks = FindHooksInHierarchy(classSymbol, "AfterAttribute", isStatic: false);
        
        // Generate invokers for each hook type
        // Implementation would follow similar pattern to GenerateTestInvoker
    }
    
    private static void GenerateCategories(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var categories = GetCategories(testInfo);
        if (categories.Any())
        {
            writer.Append("Categories = new[] { ");
            writer.Append(string.Join(", ", categories.Select(c => $"\"{c}\"")));
            writer.AppendLine(" },");
        }
        else
        {
            writer.AppendLine("Categories = Array.Empty<string>(),");
        }
    }
    
    private static void GenerateSkipStatus(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var skipAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "SkipAttribute");
        
        if (skipAttribute != null)
        {
            writer.AppendLine("IsSkipped = true,");
            var reason = skipAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "No reason provided";
            writer.AppendLine($"SkipReason = \"{reason}\",");
        }
        else
        {
            writer.AppendLine("IsSkipped = false,");
            writer.AppendLine("SkipReason = null,");
        }
    }
    
    private static void GenerateTimeout(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var timeoutAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TimeoutAttribute");
        
        if (timeoutAttribute != null && timeoutAttribute.ConstructorArguments.Length > 0)
        {
            var timeout = timeoutAttribute.ConstructorArguments[0].Value;
            writer.AppendLine($"TimeoutMs = {timeout},");
        }
        else
        {
            writer.AppendLine("TimeoutMs = null,");
        }
    }
    
    private static void GenerateDependencies(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var dependsOnAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "DependsOnAttribute");
        
        var dependencies = new List<string>();
        foreach (var attr in dependsOnAttributes)
        {
            try
            {
                if (attr.ConstructorArguments.Length > 0)
                {
                    // DependsOn can have either:
                    // 1. Just a method name (string) - assumes same class
                    // 2. A type and method name (Type, string)
                    if (attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is string methodName)
                    {
                        // Method in same class
                        dependencies.Add($"{testInfo.TypeSymbol.ToDisplayString()}.{methodName}");
                    }
                    else if (attr.ConstructorArguments.Length >= 2)
                    {
                        var depType = attr.ConstructorArguments[0].Value as ITypeSymbol;
                        var depMethod = attr.ConstructorArguments[1].Value?.ToString();
                        if (depType != null && depMethod != null)
                        {
                            dependencies.Add($"{depType.ToDisplayString()}.{depMethod}");
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Ignore attributes we can't process - they might have complex parameters
                continue;
            }
        }
        
        if (dependencies.Any())
        {
            writer.Append("DependsOn = new[] { ");
            writer.Append(string.Join(", ", dependencies.Select(d => $"\"{d}\"")));
            writer.AppendLine(" },");
        }
        else
        {
            writer.AppendLine("DependsOn = Array.Empty<string>(),");
        }
    }
    
    private static void GenerateDataSources(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // For now, we'll only handle simple data sources here
        // AsyncDataSourceGenerator attributes are handled by the old source generation pipeline
        // and should be excluded from UnifiedTestMetadataGenerator
        
        var dataSources = new List<string>();
        
        // Arguments attributes
        var argumentsAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute");
        
        foreach (var attr in argumentsAttributes)
        {
            dataSources.Add($"new StaticTestDataSource(new object?[][] {{ new object?[] {{ {FormatAttributeArguments(attr)} }} }})");
        }
        
        // MethodDataSource attributes
        var methodDataAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");
        
        foreach (var attr in methodDataAttributes)
        {
            var dataSource = GenerateDynamicDataSourceString(attr);
            if (!string.IsNullOrEmpty(dataSource))
            {
                dataSources.Add(dataSource);
            }
        }
        
        // Note: AsyncDataSourceGenerator attributes are filtered out in GetTestMethodMetadata
        // so we shouldn't see them here
        
        if (dataSources.Any())
        {
            writer.AppendLine("DataSources = new TestDataSource[]");
            writer.AppendLine("{");
            writer.Indent();
            foreach (var ds in dataSources)
            {
                writer.AppendLine($"{ds},");
            }
            writer.Unindent();
            writer.AppendLine("},");
        }
        else
        {
            writer.AppendLine("DataSources = Array.Empty<TestDataSource>(),");
        }
    }
    
    private static void GeneratePropertyDataSources(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // Find properties with data source attributes
        var properties = testInfo.TypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetAttributes().Any(a => a.AttributeClass?.Name?.EndsWith("DataSourceAttribute") == true));
        
        if (properties.Any())
        {
            writer.AppendLine("PropertyDataSources = new PropertyDataSource[]");
            writer.AppendLine("{");
            writer.Indent();
            
            foreach (var prop in properties)
            {
                var dataSourceAttr = prop.GetAttributes().First(a => a.AttributeClass?.Name?.EndsWith("DataSourceAttribute") == true);
                writer.AppendLine("new PropertyDataSource");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"PropertyName = \"{prop.Name}\",");
                writer.AppendLine($"PropertyType = typeof({prop.Type.ToDisplayString()}),");
                
                // TODO: Generate proper data source based on attribute type
                // For now, skip property data sources
                writer.AppendLine("DataSource = new StaticTestDataSource(new object?[][] { new object?[] { null } })");
                writer.Unindent();
                writer.AppendLine("},");
            }
            
            writer.Unindent();
            writer.AppendLine("},");
        }
        else
        {
            writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
        }
    }
    
    private static void GenerateParameterTypes(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // For generic methods or types, we can't generate parameter types at compile time
        if (testInfo.MethodSymbol.IsGenericMethod || testInfo.TypeSymbol.IsGenericType)
        {
            writer.AppendLine("ParameterTypes = Array.Empty<Type>(), // Generic types resolved at runtime");
            return;
        }
        
        if (testInfo.MethodSymbol.Parameters.Length > 0)
        {
            writer.Append("ParameterTypes = new Type[] { ");
            var types = testInfo.MethodSymbol.Parameters.Select(p =>
            {
                var typeName = p.Type.ToDisplayString();
                // Remove nullable annotations for typeof()
                typeName = typeName.Replace("?", "");
                return $"typeof({typeName})";
            });
            writer.Append(string.Join(", ", types));
            writer.AppendLine(" },");
        }
        else
        {
            writer.AppendLine("ParameterTypes = Array.Empty<Type>(),");
        }
    }
    
    
    private static void GenerateHooks(CodeWriter writer, TestMethodMetadata testInfo)
    {
        writer.AppendLine("Hooks = new TestHooks");
        writer.AppendLine("{");
        writer.Indent();
        
        // Generate each hook type
        writer.AppendLine("BeforeClass = Array.Empty<HookMetadata>(),");
        writer.AppendLine("AfterClass = Array.Empty<HookMetadata>(),");
        writer.AppendLine("BeforeTest = Array.Empty<HookMetadata>(),");
        writer.AppendLine("AfterTest = Array.Empty<HookMetadata>()");
        
        writer.Unindent();
        writer.AppendLine("},");
    }
    
    private static string GenerateDynamicDataSourceString(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length == 0)
        {
            return string.Empty;
        }
        
        var sourceType = attr.ConstructorArguments[0].Value as ITypeSymbol;
        var memberName = attr.ConstructorArguments.Length > 1 
            ? attr.ConstructorArguments[1].Value?.ToString() 
            : attr.ConstructorArguments[0].Value?.ToString();
            
        if (sourceType == null || memberName == null)
        {
            return string.Empty;
        }
        
        var isShared = attr.NamedArguments.FirstOrDefault(a => a.Key == "IsShared").Value.Value as bool? ?? true;
        
        return $"new DynamicTestDataSource {{ SourceType = typeof({sourceType.ToDisplayString()}), SourceMemberName = \"{memberName}\", IsShared = {isShared.ToString().ToLower()} }}";
    }
    
    private static void GenerateDynamicDataSource(CodeWriter writer, AttributeData attr)
    {
        writer.AppendLine("new DynamicTestDataSource");
        writer.AppendLine("{");
        writer.Indent();
        
        // Extract source type and member name from attribute
        if (attr.ConstructorArguments.Length >= 2)
        {
            var sourceType = attr.ConstructorArguments[0].Value as ITypeSymbol;
            var memberName = attr.ConstructorArguments[1].Value?.ToString();
            
            if (sourceType != null && memberName != null)
            {
                writer.AppendLine($"SourceType = typeof({sourceType.ToDisplayString()}),");
                writer.AppendLine($"SourceMemberName = \"{memberName}\",");
                writer.AppendLine($"IsShared = {(attr.ConstructorArguments.Length > 2 ? attr.ConstructorArguments[2].Value?.ToString() ?? "false" : "false")}");
            }
        }
        
        writer.Unindent();
        writer.AppendLine("},");
    }
    
    private static string FormatAttributeArguments(AttributeData attr)
    {
        if (attr?.ConstructorArguments == null)
        {
            return string.Empty;
        }
        
        var args = new List<string>();
        foreach (var arg in attr.ConstructorArguments)
        {
            if (arg.Kind == TypedConstantKind.Array)
            {
                // Handle params array
                if (arg.Values != null)
                {
                    foreach (var item in arg.Values)
                    {
                        if (item.Kind == TypedConstantKind.Array)
                        {
                            // Nested array - handle separately
                            args.Add(FormatTypedConstant(item));
                        }
                        else
                        {
                            args.Add(FormatAttributeArgument(item.Value));
                        }
                    }
                }
            }
            else
            {
                args.Add(FormatTypedConstant(arg));
            }
        }
        return string.Join(", ", args);
    }
    
    private static string FormatTypedConstant(TypedConstant constant)
    {
        if (constant.Kind == TypedConstantKind.Array)
        {
            var items = constant.Values.Select(v => FormatTypedConstant(v));
            return $"new[] {{ {string.Join(", ", items)} }}";
        }
        return FormatAttributeArgument(constant.Value);
    }
    
    private static string FormatAttributeArgument(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"@\"{s.Replace("\"", "\"\"")}\"",
            char c => $"'{c}'",
            bool b => b.ToString().ToLower(),
            ITypeSymbol type => $"typeof({type.ToDisplayString()})",
            _ => value.ToString() ?? "null"
        };
    }
    
    private static List<string> GetCategories(TestMethodMetadata testInfo)
    {
        var categories = new List<string>();
        
        var categoryAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "CategoryAttribute");
        
        foreach (var attr in categoryAttributes)
        {
            if (attr.ConstructorArguments.Length > 0)
            {
                var category = attr.ConstructorArguments[0].Value?.ToString();
                if (!string.IsNullOrEmpty(category))
                {
                    categories.Add(category!);
                }
            }
        }
        
        return categories;
    }
    
    private static int GetRetryCount(TestMethodMetadata testInfo)
    {
        var repeatAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RepeatAttribute");
        
        if (repeatAttribute != null && repeatAttribute.ConstructorArguments.Length > 0)
        {
            return (int)(repeatAttribute.ConstructorArguments[0].Value ?? 0);
        }
        
        return 0;
    }
    
    private static bool GetCanRunInParallel(TestMethodMetadata testInfo)
    {
        var notInParallelAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "NotInParallelAttribute");
        
        return notInParallelAttribute == null;
    }
    
    private static bool IsAsyncMethod(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        return returnType.Name == "Task" || returnType.Name == "ValueTask" ||
               (returnType is INamedTypeSymbol namedType && namedType.IsGenericType &&
                (namedType.ConstructedFrom.Name == "Task" || namedType.ConstructedFrom.Name == "ValueTask"));
    }
    
    private static List<IMethodSymbol> FindHooksInHierarchy(INamedTypeSymbol type, string attributeName, bool isStatic)
    {
        var hooks = new List<IMethodSymbol>();
        
        var currentType = type;
        while (currentType != null)
        {
            var methods = currentType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic == isStatic &&
                           m.GetAttributes().Any(a => a.AttributeClass?.Name == attributeName));
            
            hooks.AddRange(methods);
            currentType = currentType.BaseType;
        }
        
        return hooks;
    }
    
    private static void GenerateGenericTypeInfo(CodeWriter writer, TestMethodMetadata testInfo)
    {
        if (!testInfo.TypeSymbol.IsGenericType)
        {
            writer.AppendLine("GenericTypeInfo = null,");
            return;
        }
        
        var typeParams = testInfo.TypeSymbol.TypeParameters;
        if (typeParams.Length == 0)
        {
            writer.AppendLine("GenericTypeInfo = null,");
            return;
        }
        
        using (writer.BeginBlock("GenericTypeInfo = new GenericTypeInfo"))
        {
            // Parameter names
            writer.Append("ParameterNames = new[] { ");
            writer.Append(string.Join(", ", typeParams.Select(p => $"\"{p.Name}\"")));
            writer.AppendLine(" },");
            
            // Constraints
            using (writer.BeginBlock("Constraints = new GenericParameterConstraints[]"))
            {
                for (int i = 0; i < typeParams.Length; i++)
                {
                    GenerateGenericParameterConstraints(writer, typeParams[i]);
                    if (i < typeParams.Length - 1)
                    {
                        writer.AppendLine(",");
                    }
                }
            }
        }
        writer.AppendLine(",");
    }
    
    private static void GenerateGenericMethodInfo(CodeWriter writer, TestMethodMetadata testInfo)
    {
        if (!testInfo.MethodSymbol.IsGenericMethod)
        {
            writer.AppendLine("GenericMethodInfo = null");
            return;
        }
        
        var typeParams = testInfo.MethodSymbol.TypeParameters;
        if (typeParams.Length == 0)
        {
            writer.AppendLine("GenericMethodInfo = null");
            return;
        }
        
        using (writer.BeginBlock("GenericMethodInfo = new GenericMethodInfo"))
        {
            // Parameter names
            writer.Append("ParameterNames = new[] { ");
            writer.Append(string.Join(", ", typeParams.Select(p => $"\"{p.Name}\"")));
            writer.AppendLine(" },");
            
            // Constraints
            using (writer.BeginBlock("Constraints = new GenericParameterConstraints[]"))
            {
                for (int i = 0; i < typeParams.Length; i++)
                {
                    GenerateGenericParameterConstraints(writer, typeParams[i]);
                    if (i < typeParams.Length - 1)
                    {
                        writer.AppendLine(",");
                    }
                }
            }
            writer.AppendLine(",");
            
            // Parameter positions - map which method parameters use which generic types
            var positions = new List<int>();
            for (int i = 0; i < testInfo.MethodSymbol.Parameters.Length; i++)
            {
                var paramType = testInfo.MethodSymbol.Parameters[i].Type;
                if (paramType is ITypeParameterSymbol typeParam)
                {
                    var index = Array.IndexOf(typeParams.ToArray(), typeParam);
                    if (index >= 0)
                    {
                        positions.Add(i);
                    }
                }
            }
            
            if (positions.Any())
            {
                writer.Append("ParameterPositions = new[] { ");
                writer.Append(string.Join(", ", positions));
                writer.AppendLine(" }");
            }
            else
            {
                writer.AppendLine("ParameterPositions = Array.Empty<int>()");
            }
        }
    }
    
    private static void GenerateGenericParameterConstraints(CodeWriter writer, ITypeParameterSymbol param)
    {
        using (writer.BeginBlock("new GenericParameterConstraints"))
        {
            writer.AppendLine($"ParameterName = \"{param.Name}\",");
            
            // Base type constraint
            var baseConstraint = param.ConstraintTypes.FirstOrDefault(t => t.TypeKind == TypeKind.Class);
            if (baseConstraint != null)
            {
                writer.AppendLine($"BaseTypeConstraint = typeof({baseConstraint.ToDisplayString()}),");
            }
            else
            {
                writer.AppendLine("BaseTypeConstraint = null,");
            }
            
            // Interface constraints
            var interfaces = param.ConstraintTypes.Where(t => t.TypeKind == TypeKind.Interface).ToList();
            if (interfaces.Any())
            {
                writer.Append("InterfaceConstraints = new Type[] { ");
                writer.Append(string.Join(", ", interfaces.Select(i => $"typeof({i.ToDisplayString()})")));
                writer.AppendLine(" },");
            }
            else
            {
                writer.AppendLine("InterfaceConstraints = Array.Empty<Type>(),");
            }
            
            // Special constraints
            writer.AppendLine($"HasDefaultConstructorConstraint = {param.HasConstructorConstraint.ToString().ToLower()},");
            writer.AppendLine($"HasReferenceTypeConstraint = {param.HasReferenceTypeConstraint.ToString().ToLower()},");
            writer.AppendLine($"HasValueTypeConstraint = {param.HasValueTypeConstraint.ToString().ToLower()},");
            writer.AppendLine($"HasNotNullConstraint = {param.HasNotNullConstraint.ToString().ToLower()}");
        }
    }
    
    private static bool IsTestInheritedByDerivedClass(IMethodSymbol methodSymbol, INamedTypeSymbol containingType)
    {
        // If this test is defined in a class that has derived classes with [InheritsTests],
        // skip it to avoid duplicate registration
        
        // Can't determine at compile time if there are derived classes in the current compilation context
        // This would require a more complex analysis across the compilation
        // For now, return false to maintain existing behavior
        
        // A better approach would be to have the InheritsTestsGenerator skip creating metadata
        // for tests that are already handled by UnifiedTestMetadataGenerator
        return false;
    }
}