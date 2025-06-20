using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Examples;

/// <summary>
/// Example of how the TestsGenerator would be simplified to emit TestMetadata
/// instead of complex execution logic.
/// </summary>
[Generator]
public class SimplifiedTestsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var testMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.TestAttribute",
                predicate: static (_, _) => true,
                transform: static (ctx, _) => GetTestMetadata(ctx))
            .Where(static m => m is not null);

        context.RegisterSourceOutput(testMethods.Collect(), GenerateTestRegistry);
    }

    private static TestMetadataModel? GetTestMetadata(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
            return null;

        // Extract all the compile-time information
        return new TestMetadataModel
        {
            TestIdTemplate = $"{methodSymbol.ContainingType.Name}.{methodSymbol.Name}_{{TestIndex}}",
            TestClassType = methodSymbol.ContainingType,
            TestMethod = methodSymbol,
            FilePath = context.TargetNode.GetLocation().SourceTree?.FilePath ?? "",
            LineNumber = context.TargetNode.GetLocation().GetLineSpan().StartLinePosition.Line,
            DisplayNameTemplate = BuildDisplayNameTemplate(methodSymbol),
            RepeatCount = GetRepeatCount(methodSymbol),
            IsAsync = IsAsyncMethod(methodSymbol),
            IsSkipped = HasSkipAttribute(methodSymbol),
            Timeout = GetTimeout(methodSymbol),
            ClassDataSources = GetClassDataSources(methodSymbol.ContainingType),
            MethodDataSources = GetMethodDataSources(methodSymbol),
            PropertyDataSources = GetPropertyDataSources(methodSymbol.ContainingType)
        };
    }

    private static void GenerateTestRegistry(SourceProductionContext context, ImmutableArray<TestMetadataModel?> testMetadatas)
    {
        var validMetadata = testMetadatas.Where(m => m != null).Cast<TestMetadataModel>().ToList();
        if (!validMetadata.Any())
            return;

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Reflection;");
        sb.AppendLine("using TUnit.Core;");
        sb.AppendLine("using TUnit.Core.DataSources;");
        sb.AppendLine();
        sb.AppendLine("namespace TUnit.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    public static class TestMetadataRegistry");
        sb.AppendLine("    {");
        sb.AppendLine("        private static readonly List<TestMetadata> _allTestMetadata = new();");
        sb.AppendLine("        public static IReadOnlyList<TestMetadata> AllTestMetadata => _allTestMetadata;");
        sb.AppendLine();
        sb.AppendLine("        static TestMetadataRegistry()");
        sb.AppendLine("        {");

        foreach (var metadata in validMetadata)
        {
            sb.AppendLine($"            Register_{metadata.TestMethod.Name}_{metadata.TestMethod.GetHashCode()}();");
        }

        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate registration methods for each test
        foreach (var metadata in validMetadata)
        {
            GenerateTestRegistration(sb, metadata);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("TestMetadataRegistry.g.cs", sb.ToString());
    }

    private static void GenerateTestRegistration(StringBuilder sb, TestMetadataModel metadata)
    {
        var methodName = $"Register_{metadata.TestMethod.Name}_{metadata.TestMethod.GetHashCode()}";
        
        sb.AppendLine($"        private static void {methodName}()");
        sb.AppendLine("        {");
        sb.AppendLine("            var metadata = new TestMetadata");
        sb.AppendLine("            {");
        sb.AppendLine($"                TestIdTemplate = \"{metadata.TestIdTemplate}\",");
        sb.AppendLine($"                TestClassType = typeof({metadata.TestClassType.ToDisplayString()}),");
        sb.AppendLine($"                TestMethod = typeof({metadata.TestClassType.ToDisplayString()}).GetMethod(\"{metadata.TestMethod.Name}\", BindingFlags.Public | BindingFlags.Instance),");
        sb.AppendLine("                MethodMetadata = CreateMethodMetadata(),");
        sb.AppendLine($"                TestFilePath = @\"{metadata.FilePath}\",");
        sb.AppendLine($"                TestLineNumber = {metadata.LineNumber},");
        
        // Generate class factory
        GenerateClassFactory(sb, metadata);
        
        // Generate data sources
        GenerateDataSources(sb, metadata);
        
        sb.AppendLine($"                DisplayNameTemplate = \"{metadata.DisplayNameTemplate}\",");
        sb.AppendLine($"                RepeatCount = {metadata.RepeatCount},");
        sb.AppendLine($"                IsAsync = {metadata.IsAsync.ToString().ToLower()},");
        sb.AppendLine($"                IsSkipped = {metadata.IsSkipped.ToString().ToLower()},");
        
        if (metadata.SkipReason != null)
            sb.AppendLine($"                SkipReason = \"{metadata.SkipReason}\",");
        
        sb.AppendLine("                Attributes = GetTestAttributes(),");
        
        if (metadata.Timeout.HasValue)
            sb.AppendLine($"                Timeout = TimeSpan.FromMilliseconds({metadata.Timeout.Value.TotalMilliseconds})");
        else
            sb.AppendLine("                Timeout = null");
        
        sb.AppendLine("            };");
        sb.AppendLine("            _allTestMetadata.Add(metadata);");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateClassFactory(StringBuilder sb, TestMetadataModel metadata)
    {
        sb.AppendLine("                TestClassFactory = (args) =>");
        sb.AppendLine("                {");
        
        if (metadata.TestClassType.IsAbstract)
        {
            sb.AppendLine("                    return null; // Abstract class");
        }
        else
        {
            var constructors = metadata.TestClassType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic)
                .OrderBy(c => c.Parameters.Length)
                .ToList();

            if (constructors.Any())
            {
                var ctor = constructors.First();
                if (ctor.Parameters.Length == 0)
                {
                    sb.AppendLine($"                    return new {metadata.TestClassType.ToDisplayString()}();");
                }
                else
                {
                    sb.AppendLine("                    // Constructor with parameters");
                    sb.AppendLine($"                    return Activator.CreateInstance(typeof({metadata.TestClassType.ToDisplayString()}), args);");
                }
            }
            else
            {
                sb.AppendLine("                    return null; // No accessible constructor");
            }
        }
        
        sb.AppendLine("                },");
    }

    private static void GenerateDataSources(StringBuilder sb, TestMetadataModel metadata)
    {
        // Class data sources
        sb.AppendLine("                ClassDataSources = new IDataSourceProvider[]");
        sb.AppendLine("                {");
        foreach (var dataSource in metadata.ClassDataSources)
        {
            GenerateDataSourceProvider(sb, dataSource);
        }
        sb.AppendLine("                },");
        
        // Method data sources
        sb.AppendLine("                MethodDataSources = new IDataSourceProvider[]");
        sb.AppendLine("                {");
        foreach (var dataSource in metadata.MethodDataSources)
        {
            GenerateDataSourceProvider(sb, dataSource);
        }
        sb.AppendLine("                },");
        
        // Property data sources
        sb.AppendLine("                PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>");
        sb.AppendLine("                {");
        foreach (var (property, dataSource) in metadata.PropertyDataSources)
        {
            sb.AppendLine($"                    [typeof({metadata.TestClassType.ToDisplayString()}).GetProperty(\"{property.Name}\")] = ");
            GenerateDataSourceProvider(sb, dataSource, indent: "                    ");
            sb.AppendLine(",");
        }
        sb.AppendLine("                },");
    }

    private static void GenerateDataSourceProvider(StringBuilder sb, DataSourceModel dataSource, string indent = "                    ")
    {
        switch (dataSource.Type)
        {
            case DataSourceType.Inline:
                sb.AppendLine($"{indent}new InlineDataSourceProvider({string.Join(", ", dataSource.InlineValues.Select(v => FormatValue(v)))})");
                break;
                
            case DataSourceType.Method:
                sb.AppendLine($"{indent}new MethodDataSourceProvider(");
                sb.AppendLine($"{indent}    typeof({dataSource.MethodContainingType.ToDisplayString()}).GetMethod(\"{dataSource.MethodName}\"),");
                sb.AppendLine($"{indent}    instance: {(dataSource.IsStatic ? "null" : "/* need instance */")},");
                sb.AppendLine($"{indent}    isShared: {dataSource.IsShared.ToString().ToLower()})");
                break;
                
            case DataSourceType.Property:
                // Similar to method
                break;
        }
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
            return "null";
        if (value is string s)
            return $"\"{s}\"";
        if (value is bool b)
            return b.ToString().ToLower();
        // Add more type handling as needed
        return value.ToString();
    }

    // Helper methods (simplified)
    private static string BuildDisplayNameTemplate(IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
            return method.Name;
        
        var placeholders = string.Join(", ", Enumerable.Range(0, method.Parameters.Length).Select(i => $"{{{i}}}"));
        return $"{method.Name}({placeholders})";
    }

    private static int GetRepeatCount(IMethodSymbol method)
    {
        // Check for RepeatAttribute
        return 1;
    }

    private static bool IsAsyncMethod(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        return returnType.Name == "Task" || returnType.Name == "ValueTask";
    }

    private static bool HasSkipAttribute(IMethodSymbol method)
    {
        return method.GetAttributes().Any(a => a.AttributeClass?.Name == "SkipAttribute");
    }

    private static TimeSpan? GetTimeout(IMethodSymbol method)
    {
        // Check for TimeoutAttribute
        return null;
    }

    private static List<DataSourceModel> GetClassDataSources(INamedTypeSymbol classSymbol)
    {
        // Extract class-level data sources
        return new List<DataSourceModel>();
    }

    private static List<DataSourceModel> GetMethodDataSources(IMethodSymbol method)
    {
        var dataSources = new List<DataSourceModel>();
        
        // Check for Arguments attributes
        var argumentsAttributes = method.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute");
            
        foreach (var attr in argumentsAttributes)
        {
            var values = attr.ConstructorArguments.Select(a => a.Value).ToArray();
            dataSources.Add(new DataSourceModel
            {
                Type = DataSourceType.Inline,
                InlineValues = values
            });
        }
        
        // Check for MethodDataSource attributes
        // ... similar logic
        
        return dataSources;
    }

    private static Dictionary<IPropertySymbol, DataSourceModel> GetPropertyDataSources(INamedTypeSymbol classSymbol)
    {
        // Extract property data sources
        return new Dictionary<IPropertySymbol, DataSourceModel>();
    }
}

// Supporting models for the generator
internal class TestMetadataModel
{
    public string TestIdTemplate { get; set; }
    public INamedTypeSymbol TestClassType { get; set; }
    public IMethodSymbol TestMethod { get; set; }
    public string FilePath { get; set; }
    public int LineNumber { get; set; }
    public string DisplayNameTemplate { get; set; }
    public int RepeatCount { get; set; }
    public bool IsAsync { get; set; }
    public bool IsSkipped { get; set; }
    public string? SkipReason { get; set; }
    public TimeSpan? Timeout { get; set; }
    public List<DataSourceModel> ClassDataSources { get; set; } = new();
    public List<DataSourceModel> MethodDataSources { get; set; } = new();
    public Dictionary<IPropertySymbol, DataSourceModel> PropertyDataSources { get; set; } = new();
}

internal class DataSourceModel
{
    public DataSourceType Type { get; set; }
    public object?[] InlineValues { get; set; }
    public INamedTypeSymbol MethodContainingType { get; set; }
    public string MethodName { get; set; }
    public bool IsStatic { get; set; }
    public bool IsShared { get; set; }
}

internal enum DataSourceType
{
    Inline,
    Method,
    Property
}