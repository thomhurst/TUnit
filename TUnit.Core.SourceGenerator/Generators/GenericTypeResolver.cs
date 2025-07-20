using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
                if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol)
                {
                    continue;
                }

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
        if (!_processedTypes.Add(classSymbol))
        {
            return;
        }

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
        if (classSymbol.BaseType is { IsGenericType: true })
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
                // Try to infer generic types from data sources
                var inferredTypes = InferGenericTypesFromDataSources(member);

                foreach (var typeArgs in inferredTypes)
                {
                    if (typeArgs.Length <= _maxGenericDepth)
                    {
                        RegisterGenericTestInstantiation(classSymbol, typeArgs, GenericTestSource.DataSourceInference);
                    }
                }
            }
            else if (HasTestAttribute(member) && classSymbol.IsGenericType)
            {
                // For non-generic methods in generic classes, try to infer class type parameters
                var inferredTypes = InferGenericTypesFromDataSources(member);

                foreach (var typeArgs in inferredTypes)
                {
                    if (typeArgs.Length <= _maxGenericDepth)
                    {
                        RegisterGenericTestInstantiation(classSymbol, typeArgs, GenericTestSource.DataSourceInference);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Infers generic type arguments from test data sources
    /// </summary>
    public IEnumerable<ITypeSymbol[]> InferGenericTypesFromDataSources(IMethodSymbol testMethod)
    {
        var inferredTypes = new HashSet<ITypeSymbol[]>(TypeArrayComparer.Instance);

        // Infer from Arguments attributes
        var argumentsTypes = InferTypesFromArgumentsAttributes(testMethod);
        foreach (var types in argumentsTypes)
        {
            inferredTypes.Add(types);
        }

        // Infer from MethodDataSource attributes
        var methodDataSourceTypes = InferTypesFromMethodDataSourceAttributes(testMethod);
        foreach (var types in methodDataSourceTypes)
        {
            inferredTypes.Add(types);
        }

        // Infer from AsyncDataSourceGenerator attributes
        var asyncDataSourceTypes = InferTypesFromAsyncDataSourceGeneratorAttributes(testMethod);
        foreach (var types in asyncDataSourceTypes)
        {
            inferredTypes.Add(types);
        }

        return inferredTypes;
    }

    /// <summary>
    /// Infers generic types from [Arguments] attributes
    /// </summary>
    private IEnumerable<ITypeSymbol[]> InferTypesFromArgumentsAttributes(IMethodSymbol testMethod)
    {
        var argumentsAttributes = testMethod.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute" || a.AttributeClass?.Name == "Arguments");

        var inferredTypes = new HashSet<ITypeSymbol[]>(TypeArrayComparer.Instance);

        foreach (var attribute in argumentsAttributes)
        {
            var types = ExtractTypesFromArgumentsAttribute(attribute, testMethod);
            if (types.Length > 0)
            {
                inferredTypes.Add(types);
            }
        }

        return inferredTypes;
    }

    /// <summary>
    /// Infers generic types from [MethodDataSource] attributes
    /// </summary>
    private IEnumerable<ITypeSymbol[]> InferTypesFromMethodDataSourceAttributes(IMethodSymbol testMethod)
    {
        var methodDataSourceAttributes = testMethod.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");

        var inferredTypes = new HashSet<ITypeSymbol[]>(TypeArrayComparer.Instance);

        foreach (var attribute in methodDataSourceAttributes)
        {
            var types = ExtractTypesFromMethodDataSourceAttribute(attribute, testMethod);
            if (types.Length > 0)
            {
                inferredTypes.Add(types);
            }
        }

        return inferredTypes;
    }

    /// <summary>
    /// Infers generic types from AsyncDataSourceGenerator attributes
    /// </summary>
    private IEnumerable<ITypeSymbol[]> InferTypesFromAsyncDataSourceGeneratorAttributes(IMethodSymbol testMethod)
    {
        var asyncDataSourceAttributes = testMethod.GetAttributes()
            .Where(a => a.AttributeClass?.Name.Contains("AsyncDataSourceGeneratorAttribute") == true);

        var inferredTypes = new HashSet<ITypeSymbol[]>(TypeArrayComparer.Instance);

        foreach (var attribute in asyncDataSourceAttributes)
        {
            var types = ExtractTypesFromAsyncDataSourceGeneratorAttribute(attribute);
            if (types.Length > 0)
            {
                inferredTypes.Add(types);
            }
        }

        return inferredTypes;
    }

    /// <summary>
    /// Extracts type arguments from an Arguments attribute
    /// </summary>
    private ITypeSymbol[] ExtractTypesFromArgumentsAttribute(AttributeData attribute, IMethodSymbol testMethod)
    {
        if (attribute.ConstructorArguments.IsDefaultOrEmpty)
        {
            return [
            ];
        }

        var typeParameters = testMethod.TypeParameters;
        var methodParameters = testMethod.Parameters;

        // For simple cases, map argument types to generic type parameters
        var argumentTypes = new List<ITypeSymbol>();
        var constructorArgs = attribute.ConstructorArguments;

        // Handle params array - ArgumentsAttribute takes params object?[]
        // Roslyn always passes params as a single array argument
        if (constructorArgs is
            [
                { Kind: TypedConstantKind.Array } _
            ])
        {
            var arrayValues = constructorArgs[0].Values;
            for (var i = 0; i < arrayValues.Length && i < methodParameters.Length; i++)
            {
                var paramType = methodParameters[i].Type;

                // If the parameter is a generic type parameter, use the argument's actual value type
                if (paramType is ITypeParameterSymbol typeParam)
                {
                    var argValue = arrayValues[i].Value;
                    ITypeSymbol? argType = null;

                    // Determine the actual type from the value
                    if (argValue != null)
                    {
                        argType = argValue switch
                        {
                            int => testMethod.ContainingType.ContainingAssembly.GetTypeByMetadataName("System.Int32"),
                            bool => testMethod.ContainingType.ContainingAssembly.GetTypeByMetadataName("System.Boolean"),
                            string => testMethod.ContainingType.ContainingAssembly.GetTypeByMetadataName("System.String"),
                            double => testMethod.ContainingType.ContainingAssembly.GetTypeByMetadataName("System.Double"),
                            float => testMethod.ContainingType.ContainingAssembly.GetTypeByMetadataName("System.Single"),
                            long => testMethod.ContainingType.ContainingAssembly.GetTypeByMetadataName("System.Int64"),
                            _ => arrayValues[i].Type // Fall back to the declared type
                        };
                    }

                    if (argType != null)
                    {
                        // Find the corresponding type parameter in the method's type parameters
                        var typeParamIndex = Array.IndexOf(typeParameters.ToArray(), typeParam);
                        if (typeParamIndex >= 0)
                        {
                            // Ensure we have enough space in our list
                            while (argumentTypes.Count <= typeParamIndex)
                            {
                                argumentTypes.Add(null!);
                            }
                            argumentTypes[typeParamIndex] = argType;
                        }
                    }
                }
            }
        }
        else
        {
            // Handle non-params case (shouldn't happen with ArgumentsAttribute, but handle it)
            for (var i = 0; i < constructorArgs.Length && i < methodParameters.Length; i++)
            {
                var paramType = methodParameters[i].Type;

                // If the parameter is a generic type parameter, use the argument's actual value type
                if (paramType is ITypeParameterSymbol typeParam)
                {
                    var argValue = constructorArgs[i].Value;
                    ITypeSymbol? argType = null;

                    // Determine the actual type from the value
                    if (argValue != null)
                {
                    argType = argValue switch
                    {
                        int => testMethod.ContainingType.ContainingAssembly.GetTypeByMetadataName("System.Int32"),
                        bool => testMethod.ContainingType.ContainingAssembly.GetTypeByMetadataName("System.Boolean"),
                        string => testMethod.ContainingType.ContainingAssembly.GetTypeByMetadataName("System.String"),
                        double => testMethod.ContainingType.ContainingAssembly.GetTypeByMetadataName("System.Double"),
                        float => testMethod.ContainingType.ContainingAssembly.GetTypeByMetadataName("System.Single"),
                        long => testMethod.ContainingType.ContainingAssembly.GetTypeByMetadataName("System.Int64"),
                        _ => constructorArgs[i].Type // Fall back to the declared type
                    };
                }

                if (argType != null)
                {
                    // Find the corresponding type parameter in the method's type parameters
                    var typeParamIndex = Array.IndexOf(typeParameters.ToArray(), typeParam);
                    if (typeParamIndex >= 0)
                    {
                        // Ensure we have enough space in our list
                        while (argumentTypes.Count <= typeParamIndex)
                        {
                            argumentTypes.Add(null!);
                        }
                        argumentTypes[typeParamIndex] = argType;
                    }
                }
            }
        }
        }

        // Remove null entries and return
        return argumentTypes.Where(t => t != null).ToArray();
    }

    /// <summary>
    /// Extracts type arguments from a MethodDataSource attribute
    /// </summary>
    private ITypeSymbol[] ExtractTypesFromMethodDataSourceAttribute(AttributeData attribute, IMethodSymbol testMethod)
    {
        // Get method name from constructor argument
        if (attribute.ConstructorArguments.IsDefaultOrEmpty)
        {
            return [
            ];
        }

        var firstArg = attribute.ConstructorArguments[0];
        string? methodName = null;

        // Handle array case (shouldn't happen for MethodDataSourceAttribute, but be safe)
        if (firstArg.Kind == TypedConstantKind.Array)
        {
            if (firstArg.Values.Length > 0)
            {
                methodName = firstArg.Values[0].Value?.ToString();
            }
        }
        else
        {
            methodName = firstArg.Value?.ToString();
        }

        if (string.IsNullOrEmpty(methodName))
        {
            return [
            ];
        }

        var containingType = testMethod.ContainingType;
        var dataSourceMethod = containingType.GetMembers(methodName!)
            .OfType<IMethodSymbol>()
            .FirstOrDefault();

        if (dataSourceMethod?.ReturnType is not INamedTypeSymbol returnType)
        {
            return [
            ];
        }

        // Extract type from IEnumerable<T> or similar
        return ExtractGenericTypesFromEnumerableReturnType(returnType, testMethod);
    }

    /// <summary>
    /// Extracts type arguments from an AsyncDataSourceGenerator attribute
    /// </summary>
    private ITypeSymbol[] ExtractTypesFromAsyncDataSourceGeneratorAttribute(AttributeData attribute)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass?.BaseType == null)
        {
            return [
            ];
        }

        // The base type should be AsyncDataSourceGeneratorAttribute<T1, T2, ...>
        var baseType = attributeClass.BaseType;
        while (baseType != null)
        {
            if (baseType.Name.StartsWith("AsyncDataSourceGeneratorAttribute") && baseType.IsGenericType)
            {
                return baseType.TypeArguments.ToArray();
            }
            baseType = baseType.BaseType;
        }

        return [
        ];
    }

    /// <summary>
    /// Extracts generic types from enumerable return types like IEnumerable&lt;T&gt;
    /// </summary>
    private ITypeSymbol[] ExtractGenericTypesFromEnumerableReturnType(INamedTypeSymbol returnType, IMethodSymbol testMethod)
    {
        // Look for IEnumerable<T>, IEnumerable<(T1, T2)>, etc.
        var enumerableInterface = returnType.AllInterfaces
            .FirstOrDefault(i => i.Name == "IEnumerable" && i.IsGenericType);

        if (enumerableInterface?.TypeArguments.FirstOrDefault() is { } elementType)
        {
            // Handle tuple types like (T1, T2)
            if (elementType is INamedTypeSymbol { IsTupleType: true } namedType)
            {
                return namedType.TupleElements.Select(e => e.Type).ToArray();
            }

            // Handle single type T
            return [elementType];
        }

        return [
        ];
    }

    private void RegisterGenericTestInstantiation(INamedTypeSymbol originalType, ITypeSymbol[] typeArgs, GenericTestSource source)
    {
        if (_genericTests.ContainsKey(typeArgs))
        {
            return;
        }

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
        {
            return [
            ];
        }

        var typeArgs = new List<ITypeSymbol>();
        foreach (var arg in attribute.ConstructorArguments)
        {
            if (arg is { Kind: TypedConstantKind.Type, Value: ITypeSymbol typeSymbol })
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
        writer.AppendLine("TestName = \"GenericTest\",");
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
        writer.AppendLine("InstanceFactory = null, // Would need generic-aware factory");
        writer.AppendLine("TestInvoker = null, // Would need generic-aware invoker");

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
public class GenericTestInfo
{
    public required INamedTypeSymbol OriginalType { get; init; }
    public required ITypeSymbol[] TypeArguments { get; init; }
    public required GenericTestSource Source { get; init; }
    public required List<IMethodSymbol> TestMethods { get; init; }
}

/// <summary>
/// Source of generic test discovery
/// </summary>
public enum GenericTestSource
{
    ExplicitAttribute,
    ConcreteInheritance,
    CallSiteAnalysis,
    DataSourceInference
}
