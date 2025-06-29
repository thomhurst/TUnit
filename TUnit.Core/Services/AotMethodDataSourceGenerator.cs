using System.Text;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Services;

/// <summary>
/// Generates AOT-safe method data source factories that directly invoke data source methods.
/// This approach emits strongly typed code that calls MethodDataSource methods at runtime,
/// avoiding reflection while maintaining full AOT compatibility.
/// </summary>
public class AotMethodDataSourceGenerator
{
    private readonly CompileTimeSafetyAnalyzer _safetyAnalyzer;

    public AotMethodDataSourceGenerator(CompileTimeSafetyAnalyzer safetyAnalyzer)
    {
        _safetyAnalyzer = safetyAnalyzer;
    }

    /// <summary>
    /// Generates AOT-safe factory code for a MethodDataSource attribute.
    /// </summary>
    /// <param name="methodDataSourceAttribute">The MethodDataSource attribute</param>
    /// <param name="declaringType">The type that declares the data source method</param>
    /// <returns>Generated AOT-safe factory code</returns>
    public string GenerateMethodDataSourceFactory(
        MethodDataSourceAttribute methodDataSourceAttribute,
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        Type declaringType)
    {
        var methodName = methodDataSourceAttribute.MethodNameProvidingDataSource;
        var dataSourceMethod = FindDataSourceMethod(declaringType, methodName);

        if (dataSourceMethod == null)
        {
            throw new InvalidOperationException(
                $"MethodDataSource method '{methodName}' not found on type '{declaringType.FullName}'");
        }

        var isAsync = typeof(Task).IsAssignableFrom(dataSourceMethod.ReturnType);
        var factoryClassName = $"{declaringType.Name}_{methodName}_DataSourceFactory";
        var declaringTypeName = declaringType.FullName?.Replace('+', '.') ?? declaringType.Name;

        var code = new StringBuilder();
        code.AppendLine($"// Generated AOT-safe data source factory for {declaringTypeName}.{methodName}");
        code.AppendLine($"public static class {factoryClassName}");
        code.AppendLine("{");

        if (isAsync)
        {
            GenerateAsyncDataSourceFactory(code, declaringTypeName, methodName, dataSourceMethod);
        }
        else
        {
            GenerateSyncDataSourceFactory(code, declaringTypeName, methodName, dataSourceMethod);
        }

        // Generate delegate property for direct access
        GenerateFactoryDelegate(code, declaringTypeName, methodName, isAsync);

        code.AppendLine("}");
        return code.ToString();
    }

    /// <summary>
    /// Generates a synchronous data source factory method.
    /// </summary>
    private void GenerateSyncDataSourceFactory(StringBuilder code, string declaringTypeName, string methodName, System.Reflection.MethodInfo dataSourceMethod)
    {
        var returnType = GetDataSourceReturnType(dataSourceMethod);

        code.AppendLine($"    public static {returnType} GetData()");
        code.AppendLine("    {");
        code.AppendLine($"        return {declaringTypeName}.{methodName}();");
        code.AppendLine("    }");
        code.AppendLine();
    }

    /// <summary>
    /// Generates an asynchronous data source factory method.
    /// </summary>
    private void GenerateAsyncDataSourceFactory(StringBuilder code, string declaringTypeName, string methodName, System.Reflection.MethodInfo dataSourceMethod)
    {
        var returnType = GetDataSourceReturnType(dataSourceMethod);

        code.AppendLine($"    public static async Task<{returnType}> GetDataAsync()");
        code.AppendLine("    {");
        code.AppendLine($"        return await {declaringTypeName}.{methodName}();");
        code.AppendLine("    }");
        code.AppendLine();

    }

    /// <summary>
    /// Generates delegate properties for direct factory access.
    /// </summary>
    private void GenerateFactoryDelegate(StringBuilder code, string declaringTypeName, string methodName, bool isAsync)
    {
        if (isAsync)
        {
            code.AppendLine($"    public static readonly Func<Task<IEnumerable<object?[]>>> AsyncFactory = GetDataAsync;");
            code.AppendLine($"    public static readonly Func<IEnumerable<object?[]>> SyncFactory = GetData;");
        }
        else
        {
            code.AppendLine($"    public static readonly Func<IEnumerable<object?[]>> Factory = GetData;");
        }
    }

    /// <summary>
    /// Generates method data source resolver that integrates with the test variation system.
    /// </summary>
    /// <param name="testClass">The test class metadata</param>
    /// <param name="testMethod">The test method metadata</param>
    /// <returns>Generated resolver code</returns>
    public string GenerateMethodDataSourceResolver(ClassMetadata testClass, MethodMetadata testMethod)
    {
        var className = testClass.Type.Name;
        var methodName = testMethod.Name;
        var resolverClassName = $"{className}_{methodName}_MethodDataResolver";

        var code = new StringBuilder();
        code.AppendLine($"// Generated AOT-safe method data resolver for {testClass.Type.FullName}.{methodName}");
        code.AppendLine($"public static class {resolverClassName}");
        code.AppendLine("{");

        // Find all MethodDataSource attributes on the method
        var methodDataSources = testMethod.GetAttributes<MethodDataSourceAttribute>();

        code.AppendLine("    public static async Task<IReadOnlyList<object?[]>> ResolveAllMethodDataAsync()");
        code.AppendLine("    {");
        code.AppendLine("        var allData = new List<object?[]>();");
        code.AppendLine();

        foreach (var methodDataSource in methodDataSources)
        {
            var dataMethodName = methodDataSource.MethodNameProvidingDataSource;
            var factoryClassName = $"{className}_{dataMethodName}_DataSourceFactory";

            // Determine if the source method is async
            var sourceMethod = FindDataSourceMethod(testClass.Type, dataMethodName);
            var isAsync = sourceMethod != null && typeof(Task).IsAssignableFrom(sourceMethod.ReturnType);

            if (isAsync)
            {
                code.AppendLine($"        var data_{dataMethodName} = await {factoryClassName}.GetDataAsync();");
            }
            else
            {
                code.AppendLine($"        var data_{dataMethodName} = {factoryClassName}.GetData();");
            }

            code.AppendLine($"        allData.AddRange(data_{dataMethodName});");
            code.AppendLine();
        }

        code.AppendLine("        return allData.AsReadOnly();");
        code.AppendLine("    }");
        code.AppendLine();


        code.AppendLine("}");
        return code.ToString();
    }

    /// <summary>
    /// Generates registration code for method data source factories.
    /// </summary>
    /// <param name="testId">The test ID</param>
    /// <param name="testClass">The test class metadata</param>
    /// <param name="testMethod">The test method metadata</param>
    /// <returns>Generated registration code</returns>
    public string GenerateMethodDataSourceRegistration(string testId, ClassMetadata testClass, MethodMetadata testMethod)
    {
        var className = testClass.Type.Name;
        var methodName = testMethod.Name;
        var resolverClassName = $"{className}_{methodName}_MethodDataResolver";

        var code = new StringBuilder();
        code.AppendLine($"// Registration for method data sources on test {testId}");
        code.AppendLine($"GlobalSourceGeneratedTestRegistry.RegisterMethodDataResolver(\"{testId}\", {resolverClassName}.ResolveAllMethodData);");
        code.AppendLine($"GlobalSourceGeneratedTestRegistry.RegisterAsyncMethodDataResolver(\"{testId}\", {resolverClassName}.ResolveAllMethodDataAsync);");

        return code.ToString();
    }

    /// <summary>
    /// Finds a data source method on the given type.
    /// </summary>
    private static System.Reflection.MethodInfo? FindDataSourceMethod(
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        Type declaringType,
        string methodName)
    {
        return declaringType.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
    }

    /// <summary>
    /// Gets the appropriate return type for a data source method.
    /// </summary>
    private static string GetDataSourceReturnType(System.Reflection.MethodInfo method)
    {
        var returnType = method.ReturnType;

        // Handle async methods
        if (typeof(Task).IsAssignableFrom(returnType) && returnType.IsGenericType)
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        // Check if it's already the expected type
        if (typeof(IEnumerable<object[]>).IsAssignableFrom(returnType))
        {
            return "IEnumerable<object?[]>";
        }

        // Default to the most flexible type
        return "IEnumerable<object?[]>";
    }

    /// <summary>
    /// Checks if a method data source can be safely generated for AOT.
    /// </summary>
    /// <param name="methodDataSourceAttribute">The MethodDataSource attribute</param>
    /// <param name="declaringType">The type that declares the data source method</param>
    /// <returns>True if AOT-safe generation is possible</returns>
    public bool CanGenerateAotSafe(
        MethodDataSourceAttribute methodDataSourceAttribute,
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        Type declaringType)
    {
        var methodName = methodDataSourceAttribute.MethodNameProvidingDataSource;
        var dataSourceMethod = FindDataSourceMethod(declaringType, methodName);

        if (dataSourceMethod == null)
        {
            return false;
        }

        // Check if method is static (required for AOT safety)
        if (!dataSourceMethod.IsStatic)
        {
            return false;
        }

        // Check if method has parameters (not supported in this implementation)
        if (dataSourceMethod.GetParameters().Length > 0)
        {
            return false;
        }

        // Check if return type is compatible
        var returnType = dataSourceMethod.ReturnType;
        if (typeof(Task).IsAssignableFrom(returnType) && returnType.IsGenericType)
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        return typeof(IEnumerable<object[]>).IsAssignableFrom(returnType) ||
               typeof(IEnumerable<object?>).IsAssignableFrom(returnType);
    }
}
