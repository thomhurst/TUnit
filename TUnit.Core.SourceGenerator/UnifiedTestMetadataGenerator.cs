using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Models;

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

        return new TestMethodMetadata
        {
            MethodSymbol = methodSymbol,
            TypeSymbol = typeSymbol,
            FilePath = filePath,
            LineNumber = lineNumber,
            TestAttribute = context.Attributes[0],
            Context = context
        };
    }

    private static void GenerateTestRegistry(SourceProductionContext context, ImmutableArray<TestMethodMetadata?> testMethods)
    {
        var validTests = testMethods.Where(t => t != null).ToList();
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
                writer.AppendLine("RegisterAllTests();");
                writer.AppendLine();
                writer.AppendLine("// Register with the discovery service");
                writer.AppendLine("var source = new global::TUnit.Engine.SourceGeneratedTestMetadataSource(() => _allTests);");
                writer.AppendLine("global::TUnit.Engine.TestMetadataRegistry.RegisterSource(source);");
            }
            
            writer.AppendLine();
            
            // Generate the registration method
            using (writer.BeginBlock("private static void RegisterAllTests()"))
            {
                foreach (var testInfo in validTests)
                {
                    if (testInfo != null)
                    {
                        GenerateTestMetadata(writer, testInfo);
                        writer.AppendLine();
                    }
                }
            }
            
            writer.AppendLine();
            
            // Generate helper methods for each test class
            var testClasses = validTests.Where(t => t != null).GroupBy(t => t!.TypeSymbol, SymbolEqualityComparer.Default);
            foreach (var classGroup in testClasses)
            {
                if (classGroup.Key is INamedTypeSymbol namedType)
                {
                    GenerateTestClassHelpers(writer, namedType, classGroup.Cast<TestMethodMetadata>().ToList());
                }
            }
        }
        
        context.AddSource("UnifiedTestMetadataRegistry.g.cs", writer.ToString());
    }
    
    private static void GenerateTestMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var context = TestMetadataGenerationContext.Create(testInfo);
        var testId = $"{context.ClassName}.{context.MethodName}";
        
        using (writer.BeginBlock("_allTests.Add(new TestMetadata"))
        {
            writer.AppendLine($"TestId = \"{testId}\",");
            writer.AppendLine($"TestName = \"{context.MethodName}\",");
            writer.AppendLine($"TestClassType = typeof({context.ClassName}),");
            writer.AppendLine($"TestMethodName = \"{context.MethodName}\",");
            
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
            GenerateDependencies(writer, testInfo);
            
            // Data sources
            GenerateDataSources(writer, testInfo);
            
            // Property data sources
            GeneratePropertyDataSources(writer, testInfo);
            
            // Parameter info
            writer.AppendLine($"ParameterCount = {testInfo.MethodSymbol.Parameters.Length},");
            GenerateParameterTypes(writer, testInfo);
            
            // AOT factories (if possible)
            if (context.CanUseStaticDefinition && HasParameterlessConstructor(testInfo.TypeSymbol))
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
            GenerateHooks(writer, testInfo);
            
            // Reflection fallback
            if (!context.CanUseStaticDefinition)
            {
                writer.AppendLine($"MethodInfo = typeof({context.ClassName}).GetMethod(\"{context.MethodName}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance),");
            }
            else
            {
                writer.AppendLine("MethodInfo = null,");
            }
            
            // Source location
            writer.AppendLine($"FilePath = @\"{testInfo.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
            writer.AppendLine($"LineNumber = {testInfo.LineNumber}");
        }
        writer.AppendLine(");");
    }
    
    private static void GenerateTestClassHelpers(CodeWriter writer, INamedTypeSymbol classSymbol, List<TestMethodMetadata> testMethods)
    {
        var className = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        var safeClassName = className.Replace(".", "_").Replace("<", "_").Replace(">", "_");
        
        writer.AppendLine($"#region Helpers for {className}");
        writer.AppendLine();
        
        // Generate test invokers for AOT
        foreach (var testInfo in testMethods)
        {
            var context = TestMetadataGenerationContext.Create(testInfo);
            if (context.CanUseStaticDefinition && HasParameterlessConstructor(testInfo.TypeSymbol))
            {
                GenerateTestInvoker(writer, testInfo, context);
            }
        }
        
        // Generate hook invokers
        GenerateHookInvokers(writer, classSymbol);
        
        writer.AppendLine($"#endregion");
        writer.AppendLine();
    }
    
    private static void GenerateTestInvoker(CodeWriter writer, TestMethodMetadata testInfo, TestMetadataGenerationContext context)
    {
        var methodName = $"{context.SafeClassName}_{context.SafeMethodName}_Invoker";
        
        using (writer.BeginBlock($"private static async Task {methodName}(object instance, object?[] args)"))
        {
            writer.AppendLine($"var typedInstance = ({context.ClassName})instance;");
            
            // Generate parameter extraction
            for (int i = 0; i < testInfo.MethodSymbol.Parameters.Length; i++)
            {
                var param = testInfo.MethodSymbol.Parameters[i];
                var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
            if (attr.ConstructorArguments.Length > 0)
            {
                var depType = attr.ConstructorArguments[0].Value as ITypeSymbol;
                var depMethod = attr.ConstructorArguments[1].Value?.ToString();
                if (depType != null && depMethod != null)
                {
                    dependencies.Add($"{depType.ToDisplayString()}.{depMethod}");
                }
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
        writer.AppendLine("DataSources = new TestDataSource[]");
        writer.AppendLine("{");
        writer.Indent();
        
        // Arguments attributes
        var argumentsAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute");
        
        foreach (var attr in argumentsAttributes)
        {
            writer.AppendLine("new StaticTestDataSource(");
            writer.Indent();
            writer.AppendLine($"new object?[][] {{ new object?[] {{ {FormatAttributeArguments(attr)} }} }}");
            writer.Unindent();
            writer.AppendLine("),");
        }
        
        // MethodDataSource attributes
        var methodDataAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");
        
        foreach (var attr in methodDataAttributes)
        {
            GenerateDynamicDataSource(writer, attr);
        }
        
        writer.Unindent();
        writer.AppendLine("},");
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
                writer.AppendLine("DataSource = // Generate based on attribute");
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
        if (testInfo.MethodSymbol.Parameters.Length > 0)
        {
            writer.Append("ParameterTypes = new Type[] { ");
            var types = testInfo.MethodSymbol.Parameters.Select(p => $"typeof({p.Type.ToDisplayString()})");
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
        var args = new List<string>();
        foreach (var arg in attr.ConstructorArguments)
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
        return string.Join(", ", args);
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
    
    private static bool HasParameterlessConstructor(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }
        
        var constructors = namedType.Constructors;
        
        // If no constructors are explicitly defined, there's an implicit parameterless constructor
        if (!constructors.Any() || constructors.All(c => c.IsImplicitlyDeclared))
        {
            return true;
        }
        
        // Check if there's an explicit parameterless constructor
        return constructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);
    }
}