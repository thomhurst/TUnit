using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Utilities;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Resolves generic types at compile-time for AOT compatibility
/// Analyzes actually-used generic instantiations and generates metadata
/// </summary>
internal sealed class GenericTypeResolver
{
    private readonly Dictionary<ITypeSymbol[], GenericTestInfo> _genericTests;
    private readonly HashSet<INamedTypeSymbol> _processedTypes;
    private readonly int _maxGenericDepth;

    public GenericTypeResolver(int maxGenericDepth = 5)
    {
        _genericTests = new Dictionary<ITypeSymbol[], GenericTestInfo>(TypeArrayComparer.Instance);
        _processedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        _maxGenericDepth = maxGenericDepth;
    }

    /// <summary>
    /// Analyzes the compilation for generic test usage and registers discovered instantiations
    /// </summary>
    public void AnalyzeGenericUsage(Compilation compilation)
    {
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();
            
            // Find all class declarations
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                if (classSymbol == null) continue;

                AnalyzeClassForGenericTests(classSymbol, semanticModel);
            }
        }
    }

    /// <summary>
    /// Generates generic test registry code for all discovered generic instantiations
    /// </summary>
    public void GenerateGenericTestRegistry(CodeWriter writer)
    {
        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Registry for generic test instantiations discovered at compile-time");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("internal static class GenericTestRegistry");
        writer.AppendLine("{");
        writer.Indent();

        if (_genericTests.Any())
        {
            // Generate the registry dictionary
            GenerateRegistryDictionary(writer);
            
            // Generate lookup methods
            GenerateLookupMethods(writer);
            
            // Generate metadata creation methods
            GenerateMetadataCreationMethods(writer);
        }
        else
        {
            // Generate empty registry when no generic tests found
            GenerateEmptyRegistry(writer);
        }

        writer.Unindent();
        writer.AppendLine("}");
    }

    /// <summary>
    /// Gets all discovered generic test information
    /// </summary>
    public IEnumerable<GenericTestInfo> GetDiscoveredGenericTests() => _genericTests.Values;

    private void AnalyzeClassForGenericTests(INamedTypeSymbol classSymbol, SemanticModel semanticModel)
    {
        if (!_processedTypes.Add(classSymbol)) return;

        // Check for explicit generic test generation attributes
        AnalyzeExplicitGenericTestAttributes(classSymbol);

        // Check for generic test classes with concrete instantiations
        AnalyzeGenericTestClassUsage(classSymbol);

        // Analyze method-level generic tests
        AnalyzeGenericTestMethods(classSymbol);
    }

    private void AnalyzeExplicitGenericTestAttributes(INamedTypeSymbol classSymbol)
    {
        var generateGenericTestAttrs = classSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "GenerateGenericTestAttribute");

        foreach (var attr in generateGenericTestAttrs)
        {
            var typeArgs = ExtractTypeArgumentsFromAttribute(attr);
            if (typeArgs.Length > 0 && typeArgs.Length <= _maxGenericDepth)
            {
                RegisterGenericTestInstantiation(classSymbol, typeArgs, GenericTestSource.ExplicitAttribute);
            }
        }
    }

    private void AnalyzeGenericTestClassUsage(INamedTypeSymbol classSymbol)
    {
        // Check if this is a concrete instantiation of a generic test class
        if (classSymbol.BaseType != null && classSymbol.BaseType.IsGenericType)
        {
            var baseType = classSymbol.BaseType;
            if (HasTestMethods(baseType.OriginalDefinition))
            {
                var typeArgs = baseType.TypeArguments.ToArray();
                if (typeArgs.Length <= _maxGenericDepth)
                {
                    RegisterGenericTestInstantiation(baseType.OriginalDefinition, typeArgs, GenericTestSource.ConcreteInheritance);
                }
            }
        }
    }

    private void AnalyzeGenericTestMethods(INamedTypeSymbol classSymbol)
    {
        foreach (var member in classSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (member.IsGenericMethod && HasTestAttribute(member))
            {
                // For generic methods, we need explicit type arguments
                // This would require more complex analysis of call sites
                // For now, we rely on explicit attributes
            }
        }
    }

    private void RegisterGenericTestInstantiation(INamedTypeSymbol originalType, ITypeSymbol[] typeArgs, GenericTestSource source)
    {
        if (_genericTests.ContainsKey(typeArgs)) return;

        var genericTestInfo = new GenericTestInfo
        {
            OriginalType = originalType,
            TypeArguments = typeArgs,
            Source = source,
            TestMethods = GetTestMethods(originalType).ToList()
        };

        _genericTests[typeArgs] = genericTestInfo;
    }

    private ITypeSymbol[] ExtractTypeArgumentsFromAttribute(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0)
            return Array.Empty<ITypeSymbol>();

        var typeArgs = new List<ITypeSymbol>();
        foreach (var arg in attribute.ConstructorArguments)
        {
            if (arg.Kind == TypedConstantKind.Type && arg.Value is ITypeSymbol typeSymbol)
            {
                typeArgs.Add(typeSymbol);
            }
        }

        return typeArgs.ToArray();
    }

    private bool HasTestMethods(INamedTypeSymbol type)
    {
        return type.GetMembers()
            .OfType<IMethodSymbol>()
            .Any(HasTestAttribute);
    }

    private bool HasTestAttribute(IMethodSymbol method)
    {
        return method.GetAttributes().Any(a => 
            a.AttributeClass?.Name == "TestAttribute" ||
            a.AttributeClass?.Name == "Test");
    }

    private IEnumerable<IMethodSymbol> GetTestMethods(INamedTypeSymbol type)
    {
        return type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(HasTestAttribute);
    }

    private void GenerateRegistryDictionary(CodeWriter writer)
    {
        writer.AppendLine("private static readonly Dictionary<Type[], TestMetadata> Registry =");
        writer.AppendLine("    new(TypeArrayComparer.Instance)");
        writer.AppendLine("    {");
        writer.Indent();

        foreach (var kvp in _genericTests)
        {
            var typeArgs = kvp.Key;
            var testInfo = kvp.Value;
            
            writer.Append("{ new Type[] { ");
            writer.Append(string.Join(", ", typeArgs.Select(t => $"typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})")));
            writer.Append(" }, ");
            writer.Append($"CreateMetadata_{SafeTypeName(typeArgs)}() }},");
            writer.AppendLine();
        }

        writer.Unindent();
        writer.AppendLine("    };");
        writer.AppendLine();
    }

    private void GenerateLookupMethods(CodeWriter writer)
    {
        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Gets test metadata for the specified generic type arguments");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static TestMetadata? GetMetadata(params Type[] types) =>");
        writer.AppendLine("    Registry.TryGetValue(types, out var metadata) ? metadata : null;");
        writer.AppendLine();

        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Gets all registered generic test combinations");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static IEnumerable<(Type[] TypeArgs, TestMetadata Metadata)> GetAllGenericTests() =>");
        writer.AppendLine("    Registry.Select(kvp => (kvp.Key, kvp.Value));");
        writer.AppendLine();
    }

    private void GenerateMetadataCreationMethods(CodeWriter writer)
    {
        writer.AppendLine("// Metadata creation methods for each generic instantiation");
        
        foreach (var kvp in _genericTests)
        {
            var typeArgs = kvp.Key;
            var testInfo = kvp.Value;
            
            GenerateMetadataCreationMethod(writer, typeArgs, testInfo);
        }
    }

    private void GenerateMetadataCreationMethod(CodeWriter writer, ITypeSymbol[] typeArgs, GenericTestInfo testInfo)
    {
        var methodName = $"CreateMetadata_{SafeTypeName(typeArgs)}";
        var typeArgsDisplay = string.Join(", ", typeArgs.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        
        writer.AppendLine($"private static TestMetadata {methodName}() => new()");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"TestId = \"GenericTest<{typeArgsDisplay}>\",");
        writer.AppendLine($"TestName = \"GenericTest\",");
        writer.AppendLine($"TestClassType = typeof({testInfo.OriginalType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}<{typeArgsDisplay}>),");
        writer.AppendLine($"TestMethodName = \"{testInfo.TestMethods.FirstOrDefault()?.Name ?? "Unknown"}\",");
        writer.AppendLine("FilePath = string.Empty,"); // Would need syntax reference for actual file path
        writer.AppendLine("LineNumber = 0,");
        writer.AppendLine("Categories = Array.Empty<string>(),");
        writer.AppendLine("IsSkipped = false,");
        writer.AppendLine("SkipReason = null,");
        writer.AppendLine("TimeoutMs = null,");
        writer.AppendLine("RetryCount = 0,");
        writer.AppendLine("CanRunInParallel = true,");
        writer.AppendLine("DependsOn = Array.Empty<string>(),");
        writer.AppendLine("DataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("Hooks = new TestHooks");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("BeforeClass = Array.Empty<HookMetadata>(),");
        writer.AppendLine("BeforeTest = Array.Empty<HookMetadata>(),");
        writer.AppendLine("AfterTest = Array.Empty<HookMetadata>(),");
        writer.AppendLine("AfterClass = Array.Empty<HookMetadata>(),");
        writer.Unindent();
        writer.AppendLine("},");
        writer.AppendLine($"InstanceFactory = null, // Would need generic-aware factory");
        writer.AppendLine($"TestInvoker = null, // Would need generic-aware invoker");
        
        writer.Unindent();
        writer.AppendLine("};");
        writer.AppendLine();
    }

    private void GenerateEmptyRegistry(CodeWriter writer)
    {
        writer.AppendLine("private static readonly Dictionary<Type[], TestMetadata> Registry =");
        writer.AppendLine("    new(TypeArrayComparer.Instance);");
        writer.AppendLine();

        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Gets test metadata for the specified generic type arguments");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static TestMetadata? GetMetadata(params Type[] types) => null;");
        writer.AppendLine();

        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Gets all registered generic test combinations");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static IEnumerable<(Type[] TypeArgs, TestMetadata Metadata)> GetAllGenericTests() =>");
        writer.AppendLine("    Enumerable.Empty<(Type[], TestMetadata)>();");
        writer.AppendLine();
    }

    private string SafeTypeName(ITypeSymbol[] typeArgs)
    {
        return string.Join("_", typeArgs.Select(t => t.Name.Replace(".", "_")));
    }
}

/// <summary>
/// Information about a discovered generic test instantiation
/// </summary>
internal class GenericTestInfo
{
    public required INamedTypeSymbol OriginalType { get; init; }
    public required ITypeSymbol[] TypeArguments { get; init; }
    public required GenericTestSource Source { get; init; }
    public required List<IMethodSymbol> TestMethods { get; init; }
}

/// <summary>
/// Source of generic test discovery
/// </summary>
internal enum GenericTestSource
{
    ExplicitAttribute,
    ConcreteInheritance,
    CallSiteAnalysis
}