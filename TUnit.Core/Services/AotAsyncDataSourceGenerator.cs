using System.Text;
using TUnit.Core.Extensions;

namespace TUnit.Core.Services;

/// <summary>
/// Generates AOT-safe async data source factories that directly instantiate and invoke
/// AsyncDataSourceGeneratorAttribute implementations. This approach emits strongly typed code
/// that calls async data source generators at runtime, avoiding reflection while maintaining
/// full AOT compatibility.
/// </summary>
public class AotAsyncDataSourceGenerator
{
    private readonly CompileTimeSafetyAnalyzer _safetyAnalyzer;

    public AotAsyncDataSourceGenerator(CompileTimeSafetyAnalyzer safetyAnalyzer)
    {
        _safetyAnalyzer = safetyAnalyzer;
    }

    /// <summary>
    /// Generates AOT-safe factory code for an AsyncDataSourceGenerator attribute.
    /// </summary>
    /// <param name="asyncDataSourceType">The type of the AsyncDataSourceGenerator</param>
    /// <param name="declaringType">The type that declares the async data source</param>
    /// <returns>Generated AOT-safe factory code</returns>
    public string GenerateAsyncDataSourceFactory(Type asyncDataSourceType, Type declaringType)
    {
        if (!typeof(IAsyncDataSourceGeneratorAttribute).IsAssignableFrom(asyncDataSourceType))
        {
            throw new InvalidOperationException(
                $"Type {asyncDataSourceType.FullName} does not implement IAsyncDataSourceGeneratorAttribute");
        }

        var factoryClassName = $"{asyncDataSourceType.Name}_AotFactory";
        var asyncDataSourceTypeName = asyncDataSourceType.FullName?.Replace('+', '.') ?? asyncDataSourceType.Name;
        var declaringTypeName = declaringType.FullName?.Replace('+', '.') ?? declaringType.Name;

        var code = new StringBuilder();
        code.AppendLine($"// Generated AOT-safe async data source factory for {asyncDataSourceTypeName}");
        code.AppendLine($"public static class {factoryClassName}");
        code.AppendLine("{");

        // Generate factory method that creates and invokes the async data source
        GenerateAsyncDataSourceFactoryMethod(code, asyncDataSourceType, asyncDataSourceTypeName);

        // Generate delegate property for direct access
        GenerateAsyncFactoryDelegate(code, asyncDataSourceTypeName);

        code.AppendLine("}");
        return code.ToString();
    }

    /// <summary>
    /// Generates the main async data source factory method.
    /// </summary>
    private void GenerateAsyncDataSourceFactoryMethod(StringBuilder code, Type asyncDataSourceType, string asyncDataSourceTypeName)
    {
        code.AppendLine($"    public static async IAsyncEnumerable<Func<Task<object?[]?>>> GenerateDataAsync(DataGeneratorMetadata metadata, [EnumeratorCancellation] CancellationToken cancellationToken = default)");
        code.AppendLine("    {");

        // Create instance of the async data source generator
        code.AppendLine($"        var generator = new {asyncDataSourceTypeName}();");
        code.AppendLine();

        // Call the GenerateAsync method
        code.AppendLine("        await foreach (var dataSourceFunc in generator.GenerateAsync(metadata).WithCancellation(cancellationToken))");
        code.AppendLine("        {");
        code.AppendLine("            yield return dataSourceFunc;");
        code.AppendLine("        }");

        code.AppendLine("    }");
        code.AppendLine();

        // Generate sync wrapper for compatibility
        code.AppendLine($"    public static async Task<IReadOnlyList<Func<Task<object?[]?>>>> GenerateDataListAsync(DataGeneratorMetadata metadata, CancellationToken cancellationToken = default)");
        code.AppendLine("    {");
        code.AppendLine("        var results = new List<Func<Task<object?[]?>>>();");
        code.AppendLine("        await foreach (var dataSourceFunc in GenerateDataAsync(metadata, cancellationToken))");
        code.AppendLine("        {");
        code.AppendLine("            results.Add(dataSourceFunc);");
        code.AppendLine("        }");
        code.AppendLine("        return results.AsReadOnly();");
        code.AppendLine("    }");
        code.AppendLine();
    }

    /// <summary>
    /// Generates delegate properties for direct factory access.
    /// </summary>
    private void GenerateAsyncFactoryDelegate(StringBuilder code, string asyncDataSourceTypeName)
    {
        code.AppendLine($"    public static readonly Func<DataGeneratorMetadata, CancellationToken, IAsyncEnumerable<Func<Task<object?[]?>>>> AsyncFactory = GenerateDataAsync;");
        code.AppendLine($"    public static readonly Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<Func<Task<object?[]?>>>>> ListFactory = GenerateDataListAsync;");
    }

    /// <summary>
    /// Generates async data source resolver that integrates with the test variation system.
    /// </summary>
    /// <param name="testClass">The test class metadata</param>
    /// <param name="testMethod">The test method metadata</param>
    /// <returns>Generated resolver code</returns>
    public string GenerateAsyncDataSourceResolver(ClassMetadata testClass, MethodMetadata testMethod)
    {
        var className = testClass.Type.Name;
        var methodName = testMethod.Name;
        var resolverClassName = $"{className}_{methodName}_AsyncDataResolver";

        var code = new StringBuilder();
        code.AppendLine($"// Generated AOT-safe async data resolver for {testClass.Type.FullName}.{methodName}");
        code.AppendLine($"public static class {resolverClassName}");
        code.AppendLine("{");

        // Find all AsyncDataSourceGenerator attributes on the method and class
        var methodAsyncDataSources = testMethod.GetAsyncDataSourceGeneratorAttributes();
        var classAsyncDataSources = testClass.GetAsyncDataSourceGeneratorAttributes();
        var allAsyncDataSources = methodAsyncDataSources.Concat(classAsyncDataSources).ToArray();

        code.AppendLine("    public static async Task<IReadOnlyList<Func<Task<object?[]?>>>> ResolveAllAsyncDataAsync(DataGeneratorMetadata metadata, CancellationToken cancellationToken = default)");
        code.AppendLine("    {");
        code.AppendLine("        var allDataSources = new List<Func<Task<object?[]?>>>();");
        code.AppendLine();

        foreach (var asyncDataSource in allAsyncDataSources)
        {
            var sourceTypeName = asyncDataSource.GetType().Name;
            var factoryClassName = $"{sourceTypeName}_AotFactory";

            code.AppendLine($"        // Generate data from {sourceTypeName}");
            code.AppendLine($"        var data_{sourceTypeName} = await {factoryClassName}.GenerateDataListAsync(metadata, cancellationToken);");
            code.AppendLine($"        allDataSources.AddRange(data_{sourceTypeName});");
            code.AppendLine();
        }

        code.AppendLine("        return allDataSources.AsReadOnly();");
        code.AppendLine("    }");
        code.AppendLine();

        // Generate method to resolve and execute all data sources
        code.AppendLine("    public static async Task<IReadOnlyList<object?[]?>> ResolveAndExecuteAllAsyncDataAsync(DataGeneratorMetadata metadata, CancellationToken cancellationToken = default)");
        code.AppendLine("    {");
        code.AppendLine("        var dataSources = await ResolveAllAsyncDataAsync(metadata, cancellationToken);");
        code.AppendLine("        var results = new List<object?[]?>();");
        code.AppendLine();
        code.AppendLine("        foreach (var dataSource in dataSources)");
        code.AppendLine("        {");
        code.AppendLine("            try");
        code.AppendLine("            {");
        code.AppendLine("                var result = await dataSource();");
        code.AppendLine("                results.Add(result);");
        code.AppendLine("            }");
        code.AppendLine("            catch (Exception ex)");
        code.AppendLine("            {");
        code.AppendLine("                // Log error and continue with other data sources");
        code.AppendLine("                System.Diagnostics.Debug.WriteLine($\\\"Error executing async data source: {ex.Message}\\\");");
        code.AppendLine("            }");
        code.AppendLine("        }");
        code.AppendLine();
        code.AppendLine("        return results.AsReadOnly();");
        code.AppendLine("    }");

        code.AppendLine("}");
        return code.ToString();
    }

    /// <summary>
    /// Generates registration code for async data source factories.
    /// </summary>
    /// <param name="testId">The test ID</param>
    /// <param name="testClass">The test class metadata</param>
    /// <param name="testMethod">The test method metadata</param>
    /// <returns>Generated registration code</returns>
    public string GenerateAsyncDataSourceRegistration(string testId, ClassMetadata testClass, MethodMetadata testMethod)
    {
        var className = testClass.Type.Name;
        var methodName = testMethod.Name;
        var resolverClassName = $"{className}_{methodName}_AsyncDataResolver";

        var code = new StringBuilder();
        code.AppendLine($"// Registration for async data sources on test {testId}");
        code.AppendLine($"global::TUnit.Core.Services.TestExecutionRegistry.Instance.RegisterAsyncDataSourceResolver(\\\"{testId}\\\", {resolverClassName}.ResolveAllAsyncDataAsync);");
        code.AppendLine($"global::TUnit.Core.Services.TestExecutionRegistry.Instance.RegisterAsyncDataExecutor(\\\"{testId}\\\", {resolverClassName}.ResolveAndExecuteAllAsyncDataAsync);");

        return code.ToString();
    }

    /// <summary>
    /// Generates all AsyncDataSource factories for a test class.
    /// </summary>
    /// <param name="classMetadata">The test class metadata</param>
    /// <param name="testMethods">The test methods in the class</param>
    /// <returns>Generated AsyncDataSource factory code</returns>
    public string GenerateAsyncDataSourceFactories(ClassMetadata classMetadata, IEnumerable<MethodMetadata> testMethods)
    {
        var code = new StringBuilder();
        var className = classMetadata.Type.Name;
        var fullClassName = classMetadata.Type.FullName?.Replace('+', '.') ?? className;

        code.AppendLine($"// Generated AOT-safe AsyncDataSource factories for {fullClassName}");
        code.AppendLine();

        // Collect all unique AsyncDataSource types across all test methods and the class
        var allAsyncDataSourceTypes = new HashSet<Type>();

        // Class-level async data sources
        var classAsyncDataSources = classMetadata.GetAsyncDataSourceGeneratorAttributes();
        foreach (var asyncDataSource in classAsyncDataSources)
        {
            allAsyncDataSourceTypes.Add(asyncDataSource.GetType());
        }

        // Method-level async data sources
        foreach (var method in testMethods)
        {
            var methodAsyncDataSources = method.GetAsyncDataSourceGeneratorAttributes();
            foreach (var asyncDataSource in methodAsyncDataSources)
            {
                allAsyncDataSourceTypes.Add(asyncDataSource.GetType());
            }
        }

        // Generate factories for each unique async data source type
        foreach (var asyncDataSourceType in allAsyncDataSourceTypes)
        {
            try
            {
                var factoryCode = GenerateAsyncDataSourceFactory(asyncDataSourceType, classMetadata.Type);
                code.AppendLine(factoryCode);
                code.AppendLine();
            }
            catch (Exception ex)
            {
                code.AppendLine($"// Error generating AsyncDataSource factory for {asyncDataSourceType.Name}: {ex.Message}");
                code.AppendLine();
            }
        }

        // Generate resolvers for each test method that has AsyncDataSource attributes
        foreach (var method in testMethods)
        {
            var methodAsyncDataSources = method.GetAsyncDataSourceGeneratorAttributes();
            var classAsyncDataSources2 = classMetadata.GetAsyncDataSourceGeneratorAttributes();

            if (methodAsyncDataSources.Any() || classAsyncDataSources2.Any())
            {
                try
                {
                    var resolverCode = GenerateAsyncDataSourceResolver(classMetadata, method);
                    code.AppendLine(resolverCode);
                    code.AppendLine();
                }
                catch (Exception ex)
                {
                    code.AppendLine($"// Error generating AsyncDataSource resolver for {method.Name}: {ex.Message}");
                    code.AppendLine();
                }
            }
        }

        return code.ToString();
    }

    /// <summary>
    /// Checks if an async data source can be safely generated for AOT.
    /// </summary>
    /// <param name="asyncDataSourceType">The type of the AsyncDataSourceGenerator</param>
    /// <returns>True if AOT-safe generation is possible</returns>
    public bool CanGenerateAotSafe(
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type asyncDataSourceType)
    {
        // Check if type implements the required interface
        if (!typeof(IAsyncDataSourceGeneratorAttribute).IsAssignableFrom(asyncDataSourceType))
        {
            return false;
        }

        // Check if type has a parameterless constructor (required for instantiation)
        var constructor = asyncDataSourceType.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
        {
            return false;
        }

        // Check if type is not abstract
        if (asyncDataSourceType.IsAbstract)
        {
            return false;
        }

        return true;
    }
}
