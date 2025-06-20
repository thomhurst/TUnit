using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

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

        // Collect all test methods and generate registration
        var collected = testMethods.Collect();
        context.RegisterSourceOutput(collected, GenerateTestRegistration);
    }

    private static TestMethodInfo? GetTestMethodMetadata(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        var typeSymbol = methodSymbol.ContainingType;

        // Skip abstract classes and static methods
        if (typeSymbol.IsAbstract || methodSymbol.IsStatic)
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

    private static void GenerateTestRegistration(SourceProductionContext context, ImmutableArray<TestMethodInfo?> testMethods)
    {
        var validTests = testMethods.Where(t => t != null).Cast<TestMethodInfo>().ToList();
        if (!validTests.Any())
        {
            return;
        }

        var sourceBuilder = new SourceCodeWriter();

        // Generate file header
        sourceBuilder.Write("#nullable enable");
        sourceBuilder.Write("using System;");
        sourceBuilder.Write("using System.Collections.Generic;");
        sourceBuilder.Write("using System.Reflection;");
        sourceBuilder.Write("using TUnit.Core;");
        sourceBuilder.Write("using TUnit.Core.DataSources;");
        sourceBuilder.Write("using TUnit.Core.SourceGenerator;");
        sourceBuilder.WriteLine();
        sourceBuilder.Write("namespace TUnit.Generated;");
        sourceBuilder.WriteLine();
        sourceBuilder.Write("file static class TestMetadataRegistry");
        sourceBuilder.Write("{");
        sourceBuilder.Write("    [System.Runtime.CompilerServices.ModuleInitializer]");
        sourceBuilder.Write("    public static void Initialize()");
        sourceBuilder.Write("    {");
        sourceBuilder.Write("        var testMetadata = new List<TestMetadata>();");
        sourceBuilder.WriteLine();

        // Generate metadata for each test
        int testIndex = 0;
        foreach (var testInfo in validTests)
        {
            GenerateTestMetadata(sourceBuilder, testInfo, testIndex++);
        }

        sourceBuilder.WriteLine();
        sourceBuilder.Write("        TestSourceRegistrar.RegisterMetadata(testMetadata);");
        sourceBuilder.Write("    }");

        // Generate helper methods
        foreach (var testInfo in validTests.Select((t, i) => new { Test = t, Index = i }))
        {
            GenerateHelperMethods(sourceBuilder, testInfo.Test, testInfo.Index);
        }

        sourceBuilder.Write("}");

        context.AddSource("TestMetadataRegistry.g.cs", sourceBuilder.ToString());
    }

    private static void GenerateTestMetadata(SourceCodeWriter sb, TestMethodInfo testInfo, int index)
    {
        var className = GetFullTypeName(testInfo.TypeSymbol);
        var methodName = testInfo.MethodSymbol.Name;

        sb.Write($"// {className}.{methodName}");
        sb.Write($"testMetadata.Add(CreateTestMetadata_{index}());");
    }

    private static void GenerateHelperMethods(SourceCodeWriter sourceCodeWriter, TestMethodInfo testInfo, int index)
    {
        var className = GetFullTypeName(testInfo.TypeSymbol);
        var methodName = testInfo.MethodSymbol.Name;

        sourceCodeWriter.WriteLine();
        sourceCodeWriter.Write($"    private static TestMetadata CreateTestMetadata_{index}()");
        sourceCodeWriter.Write("    {");
        sourceCodeWriter.Write("        return new TestMetadata");
        sourceCodeWriter.Write("        {");
        sourceCodeWriter.Write($"            TestIdTemplate = \"{className}.{methodName}_{{{{TestIndex}}}}\",");
        sourceCodeWriter.Write($"            TestClassType = typeof({className}),");
        sourceCodeWriter.Write($"            TestMethod = typeof({className}).GetMethod(\"{methodName}\", BindingFlags.Public | BindingFlags.Instance),");
        sourceCodeWriter.Write($"            MethodMetadata = CreateMethodMetadata_{index}(),");
        sourceCodeWriter.Write($"            TestFilePath = @\"{testInfo.FilePath}\",");
        sourceCodeWriter.Write($"            TestLineNumber = {testInfo.LineNumber},");
        sourceCodeWriter.Write($"            TestClassFactory = CreateClassFactory_{index}(),");
        sourceCodeWriter.Write($"            ClassDataSources = GetClassDataSources_{index}(),");
        sourceCodeWriter.Write($"            MethodDataSources = GetMethodDataSources_{index}(),");
        sourceCodeWriter.Write($"            PropertyDataSources = GetPropertyDataSources_{index}(),");
        sourceCodeWriter.Write($"            DisplayNameTemplate = \"{methodName}\",");
        sourceCodeWriter.Write($"            RepeatCount = {GetRepeatCount(testInfo)},");
        sourceCodeWriter.Write($"            IsAsync = {(IsAsyncMethod(testInfo.MethodSymbol) ? "true" : "false")},");
        sourceCodeWriter.Write($"            IsSkipped = {(IsSkipped(testInfo) ? "true" : "false")},");

        var skipReason = GetSkipReason(testInfo);
        if (skipReason != null)
        {
            sourceCodeWriter.Write($"            SkipReason = \"{skipReason}\",");
        }

        sourceCodeWriter.Write("            Attributes = Array.Empty<Attribute>(),");

        var timeout = GetTimeout(testInfo);
        if (timeout.HasValue)
        {
            sourceCodeWriter.Write($"            Timeout = TimeSpan.FromMilliseconds({timeout.Value})");
        }
        else
        {
            sourceCodeWriter.Write("            Timeout = null");
        }

        sourceCodeWriter.Write("}");
        sourceCodeWriter.Write(";");
        sourceCodeWriter.Write("}");

        // Generate MethodMetadata helper
        GenerateMethodMetadataHelper(sourceCodeWriter, testInfo, index);

        // Generate class factory
        GenerateClassFactoryHelper(sourceCodeWriter, testInfo, index);

        // Generate data source helpers
        GenerateDataSourceHelpers(sourceCodeWriter, testInfo, index);
    }

    private static void GenerateMethodMetadataHelper(SourceCodeWriter sb, TestMethodInfo testInfo, int index)
    {
        var className = GetFullTypeName(testInfo.TypeSymbol);

        sb.WriteLine();
        sb.Write($"    private static MethodMetadata CreateMethodMetadata_{index}()");
        sb.Write("    {");
        sb.Write("        return new MethodMetadata");
        sb.Write("        {");
        sb.Write($"            Name = \"{testInfo.MethodSymbol.Name}\",");
        sb.Write($"            Type = typeof({className}),");

        // Generate parameter metadata
        sb.Write("            Parameters = new ParameterMetadata[]");
        sb.Write("            {");
        foreach (var param in testInfo.MethodSymbol.Parameters)
        {
            sb.Write($"                new ParameterMetadata(typeof({GetFullTypeName(param.Type)}))");
            sb.Write("                {");
            sb.Write($"                    Name = \"{param.Name}\",");
            sb.Write("                    Attributes = Array.Empty<AttributeMetadata>(),");
            sb.Write($"                    ReflectionInfo = typeof({GetFullTypeName(testInfo.TypeSymbol)}).GetMethod(\"{testInfo.MethodSymbol.Name}\")!.GetParameters()[{param.Ordinal}]");
            sb.Write("                },");
        }
        sb.Write("            },");

        sb.Write($"            GenericTypeCount = {testInfo.MethodSymbol.TypeParameters.Length},");
        sb.Write("            Class = new ClassMetadata");
        sb.Write("            {");
        sb.Write($"                Name = \"{testInfo.TypeSymbol.Name}\",");
        sb.Write($"                Type = typeof({className}),");
        sb.Write("                Attributes = Array.Empty<AttributeMetadata>(),");
        sb.Write($"                Namespace = \"{testInfo.TypeSymbol.ContainingNamespace}\",");
        sb.Write($"                Assembly = new AssemblyMetadata {{ Name = \"{testInfo.TypeSymbol.ContainingAssembly.Name}\", Attributes = Array.Empty<AttributeMetadata>() }},");

        // Generate constructor parameters
        var constructors = testInfo.TypeSymbol.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        if (constructors.Any())
        {
            var primaryConstructor = constructors.OrderBy(c => c.Parameters.Length).First();
            sb.Write("                Parameters = new ParameterMetadata[]");
            sb.Write("                {");
            foreach (var param in primaryConstructor.Parameters)
            {
                sb.Write($"                    new ParameterMetadata(typeof({GetFullTypeName(param.Type)}))");
                sb.Write("                    {");
                sb.Write($"                        Name = \"{param.Name}\",");
                sb.Write("                        Attributes = Array.Empty<AttributeMetadata>(),");
                sb.Write($"                        ReflectionInfo = typeof({GetFullTypeName(testInfo.TypeSymbol)}).GetConstructors()[0].GetParameters()[{param.Ordinal}]");
                sb.Write("                    },");
            }
            sb.Write("                },");
        }
        else
        {
            sb.Write("                Parameters = Array.Empty<ParameterMetadata>(),");
        }

        sb.Write("                Properties = Array.Empty<PropertyMetadata>(),");
        sb.Write("                Constructors = Array.Empty<ConstructorMetadata>(),");
        sb.Write("                Parent = null");
        sb.Write("            },");
        sb.Write($"            ReturnType = typeof({GetReturnTypeName(testInfo.MethodSymbol)}),");
        sb.Write("            Attributes = Array.Empty<AttributeMetadata>()");
        sb.Write("        };");
        sb.Write("    }");
    }

    private static void GenerateClassFactoryHelper(SourceCodeWriter sb, TestMethodInfo testInfo, int index)
    {
        var className = GetFullTypeName(testInfo.TypeSymbol);

        sb.WriteLine();
        sb.Write($"    private static Func<object?[]?, object?> CreateClassFactory_{index}()");
        sb.Write("    {");
        sb.Write("        return args =>");
        sb.Write("        {");

        // Get public constructors
        var constructors = testInfo.TypeSymbol.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .OrderBy(c => c.Parameters.Length)
            .ToList();

        if (!constructors.Any())
        {
            // No public constructors - use Activator
            sb.Write($"            return Activator.CreateInstance(typeof({className}), args);");
        }
        else
        {
            var primaryConstructor = constructors.First();

            if (primaryConstructor.Parameters.Length == 0)
            {
                // Parameterless constructor
                sb.Write($"            return new {className}();");
            }
            else
            {
                // Constructor with parameters
                sb.Write("            if (args == null || args.Length == 0)");
                sb.Write("            {");

                // Check if all parameters have defaults
                var allHaveDefaults = primaryConstructor.Parameters.All(p => p.HasExplicitDefaultValue);
                if (allHaveDefaults)
                {
                    var defaultArgs = string.Join(", ",
                        primaryConstructor.Parameters.Select(p => FormatValue(p.ExplicitDefaultValue)));
                    sb.Write($"                return new {className}({defaultArgs});");
                }
                else
                {
                    sb.Write($"                throw new InvalidOperationException(\"Constructor for {className} requires {primaryConstructor.Parameters.Length} arguments\");");
                }

                sb.Write("            }");
                sb.WriteLine();

                // Generate constructor call with args
                sb.Write($"            return new {className}(");
                for (int i = 0; i < primaryConstructor.Parameters.Length; i++)
                {
                    var param = primaryConstructor.Parameters[i];
                    var comma = i < primaryConstructor.Parameters.Length - 1 ? "," : "";
                    sb.Write($"                ({GetFullTypeName(param.Type)})args[{i}]{comma}");
                }
                sb.Write("            );");
            }
        }

        sb.Write("        };");
        sb.Write("    }");
    }

    private static void GenerateDataSourceHelpers(SourceCodeWriter sb, TestMethodInfo testInfo, int index)
    {
        var className = GetFullTypeName(testInfo.TypeSymbol);

        // Class data sources
        sb.WriteLine();
        sb.Write($"    private static IReadOnlyList<IDataSourceProvider> GetClassDataSources_{index}()");
        sb.Write("    {");

        var classDataSources = GetClassDataSources(testInfo);
        if (classDataSources.Any())
        {
            sb.Write("        return new IDataSourceProvider[]");
            sb.Write("        {");
            foreach (var source in classDataSources)
            {
                sb.Write($"            {source},");
            }
            sb.Write("        };");
        }
        else
        {
            sb.Write("        return Array.Empty<IDataSourceProvider>();");
        }

        sb.Write("    }");

        // Method data sources
        sb.WriteLine();
        sb.Write($"    private static IReadOnlyList<IDataSourceProvider> GetMethodDataSources_{index}()");
        sb.Write("    {");

        var methodDataSources = GetMethodDataSources(testInfo);
        if (methodDataSources.Any())
        {
            sb.Write("        return new IDataSourceProvider[]");
            sb.Write("        {");
            foreach (var source in methodDataSources)
            {
                sb.Write($"            {source},");
            }
            sb.Write("        };");
        }
        else
        {
            sb.Write("        return Array.Empty<IDataSourceProvider>();");
        }

        sb.Write("    }");

        // Property data sources
        sb.WriteLine();
        sb.Write($"    private static IReadOnlyDictionary<PropertyInfo, IDataSourceProvider> GetPropertyDataSources_{index}()");
        sb.Write("    {");

        var propertyDataSources = GetPropertyDataSources(testInfo);
        if (propertyDataSources.Any())
        {
            sb.Write("        var dict = new Dictionary<PropertyInfo, IDataSourceProvider>();");
            foreach (var (propName, source) in propertyDataSources)
            {
                sb.Write($"        dict[typeof({className}).GetProperty(\"{propName}\")] = {source};");
            }
            sb.Write("        return dict;");
        }
        else
        {
            sb.Write("        return new Dictionary<PropertyInfo, IDataSourceProvider>();");
        }

        sb.Write("    }");
    }

    private static string GetFullTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
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

    private static int GetRepeatCount(TestMethodInfo testInfo)
    {
        var repeatAttr = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "TUnit.Core.RepeatAttribute");

        if (repeatAttr != null && repeatAttr.ConstructorArguments.Length > 0)
        {
            var count = repeatAttr.ConstructorArguments[0].Value;
            if (count is int repeatCount && repeatCount > 0)
            {
                return repeatCount;
            }
        }

        return 1;
    }

    private static bool IsSkipped(TestMethodInfo testInfo)
    {
        return testInfo.MethodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == "TUnit.Core.SkipAttribute");
    }

    private static string? GetSkipReason(TestMethodInfo testInfo)
    {
        var skipAttr = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "TUnit.Core.SkipAttribute");

        if (skipAttr != null && skipAttr.ConstructorArguments.Length > 0)
        {
            return skipAttr.ConstructorArguments[0].Value?.ToString();
        }

        return null;
    }

    private static int? GetTimeout(TestMethodInfo testInfo)
    {
        var timeoutAttr = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "TUnit.Core.TimeoutAttribute");

        if (timeoutAttr != null && timeoutAttr.ConstructorArguments.Length > 0)
        {
            var timeout = timeoutAttr.ConstructorArguments[0].Value;
            if (timeout is int milliseconds && milliseconds > 0)
            {
                return milliseconds;
            }
        }

        return null;
    }

    private static List<object?[]> GetArgumentsAttributes(TestMethodInfo testInfo)
    {
        var result = new List<object?[]>();

        // Find ArgumentsAttribute on the method
        var argumentsAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == "TUnit.Core.ArgumentsAttribute");

        foreach (var attr in argumentsAttributes)
        {
            var args = attr.ConstructorArguments
                .SelectMany<TypedConstant, TypedConstant>(a => a.Kind == TypedConstantKind.Array ? a.Values : new[] { a })
                .Select(a => a.Value)
                .ToArray();
            result.Add(args);
        }

        return result;
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
        {
            return "null";
        }
        if (value is string s)
        {
            return $"\"{s.Replace("\"", "\\\"")}\"";
        }
        if (value is bool b)
        {
            return b.ToString().ToLower();
        }
        if (value is char c)
        {
            return $"'{c}'";
        }
        if (value is ITypeSymbol typeSymbol)
        {
            return $"typeof({GetFullTypeName(typeSymbol)})";
        }

        return value.ToString() ?? "null";
    }

    private static List<string> GetClassDataSources(TestMethodInfo testInfo)
    {
        var sources = new List<string>();

        // Check for ClassDataSource attributes on the class
        var classDataSourceAttrs = testInfo.TypeSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == "TUnit.Core.ClassDataSourceAttribute");

        foreach (var attr in classDataSourceAttrs)
        {
            if (attr.ConstructorArguments.Length > 0)
            {
                var sourceType = attr.ConstructorArguments[0].Value as ITypeSymbol;
                if (sourceType != null)
                {
                    var typeName = GetFullTypeName(sourceType);
                    var sharedStr = attr.NamedArguments.FirstOrDefault(n => n.Key == "Shared").Value.Value?.ToString()?.ToLower() ?? "false";
                    sources.Add($"new EnumerableDataSourceProvider(typeof({typeName}), {sharedStr})");
                }
            }
        }

        // Check for ClassConstructor attributes
        var constructorAttrs = testInfo.TypeSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == "TUnit.Core.ClassConstructorAttribute");

        foreach (var attr in constructorAttrs)
        {
            var args = attr.ConstructorArguments
                .SelectMany<TypedConstant, TypedConstant>(a => a.Kind == TypedConstantKind.Array ? a.Values : new[] { a })
                .Select(a => a.Value)
                .ToArray();

            if (args.Length > 0)
            {
                sources.Add($"new InlineDataSourceProvider({string.Join(", ", args.Select(FormatValue))})");
            }
        }

        return sources;
    }

    private static List<string> GetMethodDataSources(TestMethodInfo testInfo)
    {
        var sources = new List<string>();

        // Get inline Arguments attributes
        var argumentsAttributes = GetArgumentsAttributes(testInfo);
        foreach (var args in argumentsAttributes)
        {
            sources.Add($"new InlineDataSourceProvider({string.Join(", ", args.Select(FormatValue))})");
        }

        // Get MethodDataSource attributes
        var methodDataSourceAttrs = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == "TUnit.Core.MethodDataSourceAttribute");

        foreach (var attr in methodDataSourceAttrs)
        {
            if (attr.ConstructorArguments.Length >= 1)
            {
                var methodName = attr.ConstructorArguments[0].Value?.ToString();
                if (!string.IsNullOrEmpty(methodName))
                {
                    var declaringType = attr.ConstructorArguments.Length >= 2
                        ? attr.ConstructorArguments[1].Value as ITypeSymbol
                        : testInfo.TypeSymbol;

                    var typeName = GetFullTypeName(declaringType ?? testInfo.TypeSymbol);
                    sources.Add($"new MethodDataSourceProvider(typeof({typeName}), \"{methodName}\")");
                }
            }
        }

        return sources;
    }

    private static List<(string PropertyName, string Source)> GetPropertyDataSources(TestMethodInfo testInfo)
    {
        var sources = new List<(string, string)>();

        // Check properties for data source attributes
        var properties = testInfo.TypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public);

        foreach (var property in properties)
        {
            // Check for Arguments attribute on property
            var argsAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "TUnit.Core.ArgumentsAttribute");

            if (argsAttr != null)
            {
                var args = argsAttr.ConstructorArguments
                    .SelectMany<TypedConstant, TypedConstant>(a => a.Kind == TypedConstantKind.Array ? a.Values : new[] { a })
                    .Select(a => a.Value)
                    .ToArray();

                if (args.Length > 0)
                {
                    sources.Add((property.Name, $"new InlineDataSourceProvider({string.Join(", ", args.Select(FormatValue))})"));
                }
            }

            // Check for MethodDataSource attribute on property
            var methodDataSourceAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "TUnit.Core.MethodDataSourceAttribute");

            if (methodDataSourceAttr != null && methodDataSourceAttr.ConstructorArguments.Length >= 1)
            {
                var methodName = methodDataSourceAttr.ConstructorArguments[0].Value?.ToString();
                if (!string.IsNullOrEmpty(methodName))
                {
                    var declaringType = methodDataSourceAttr.ConstructorArguments.Length >= 2
                        ? methodDataSourceAttr.ConstructorArguments[1].Value as ITypeSymbol
                        : testInfo.TypeSymbol;

                    var typeName = GetFullTypeName(declaringType ?? testInfo.TypeSymbol);
                    sources.Add((property.Name, $"new MethodDataSourceProvider(typeof({typeName}), \"{methodName}\")"));
                }
            }
        }

        return sources;
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
