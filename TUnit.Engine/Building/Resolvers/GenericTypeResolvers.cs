using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Discovery;

namespace TUnit.Engine.Building.Resolvers;

/// <summary>
/// Resolves generic types in test metadata for reflection mode.
/// This class handles runtime expansion of generic tests based on test data.
/// </summary>
public sealed class ReflectionGenericTypeResolver : IGenericTypeResolver
{
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling", Justification = "This resolver is only used in reflection mode")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' may break functionality when trimming application code", Justification = "Generic expansion in reflection mode requires dynamic type access which is expected in this mode")]
    public async Task<IEnumerable<TestMetadata>> ResolveGenericsAsync(IEnumerable<TestMetadata> metadata)
    {
        Console.WriteLine($"ReflectionGenericTypeResolver.ResolveGenericsAsync called with {metadata.Count()} tests");
        
        var resolvedTests = new List<TestMetadata>();

        foreach (var test in metadata)
        {
            if (!HasGenericTypes(test))
            {
                // No generics to resolve
                resolvedTests.Add(test);
                continue;
            }

            try
            {
                // In reflection mode, we need to expand generic tests
                var expandedTests = await ExpandGenericTestAsync(test);
                resolvedTests.AddRange(expandedTests);
            }
            catch (GenericTypeResolutionException ex)
            {
                // Log the error but don't crash the entire test run
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Skipping generic test '{test.TestName}' in reflection mode.");
                // Don't add the test to resolved tests - it will be skipped
            }
            catch (Exception ex)
            {
                // Log unexpected errors but continue
                Console.WriteLine($"ERROR: Unexpected error resolving generic test '{test.TestName}': {ex.Message}");
                // Don't add the test to resolved tests - it will be skipped
            }
        }

        return resolvedTests;
    }

    private static bool HasGenericTypes(TestMetadata test)
    {
        return test.GenericTypeInfo != null || test.GenericMethodInfo != null;
    }

    [RequiresDynamicCode("Generic type resolution requires dynamic code generation")]
    [RequiresUnreferencedCode("Generic type resolution may access types not preserved by trimming")]
    private async Task<IEnumerable<TestMetadata>> ExpandGenericTestAsync(TestMetadata genericTest)
    {
        // For generic tests, we need test data to infer types
        // This is a simplified version - full implementation would need to:
        // 1. Get initial data from data sources
        // 2. Infer generic types from the data
        // 3. Create specialized metadata for each type combination

        // Check if we have data sources by looking at the DataSources property
        // or by checking if it's a ReflectionTestMetadata which handles data sources dynamically
        var hasDataSources = genericTest.DataSources.Length > 0 || 
                            genericTest.ClassDataSources.Length > 0 || 
                            genericTest.PropertyDataSources.Length > 0;

        // For ReflectionTestMetadata, we also need to check if there are data source attributes
        // since they're not populated in the DataSources property
        if (!hasDataSources && genericTest is ReflectionTestMetadata reflectionTest)
        {
            hasDataSources = HasDataSourceAttributes(reflectionTest);
        }

        if (!hasDataSources && genericTest.GenericMethodInfo != null)
        {
            Console.WriteLine($"DEBUG: Generic test '{genericTest.TestName}' has no data sources. " +
                              $"DataSources.Length: {genericTest.DataSources.Length}, " +
                              $"ClassDataSources.Length: {genericTest.ClassDataSources.Length}, " +
                              $"PropertyDataSources.Length: {genericTest.PropertyDataSources.Length}, " +
                              $"IsReflectionTestMetadata: {genericTest is ReflectionTestMetadata}");
            
            throw new GenericTypeResolutionException(
                $"Generic test method '{genericTest.TestName}' requires test data to infer generic type parameters. " +
                "Add [Arguments] attributes or other data sources.");
        }

        // For reflection mode, generic tests are not fully supported yet
        // Return the original test with a marker that it couldn't be expanded
        Console.WriteLine($"WARNING: Generic test '{genericTest.TestName}' cannot be expanded in reflection mode. " +
                          "Use source generation mode for full generic test support.");
        
        // Return empty to skip the test
        return await Task.FromResult(Enumerable.Empty<TestMetadata>());
    }

    /// <summary>
    /// Checks if a ReflectionTestMetadata has data source attributes that can provide type information
    /// </summary>
    [RequiresDynamicCode("Reflection-based data source detection requires dynamic code generation")]
    [RequiresUnreferencedCode("Reflection-based data source detection may access types not preserved by trimming")]
    private bool HasDataSourceAttributes(ReflectionTestMetadata reflectionTest)
    {
        try
        {
            // Use reflection to get the private fields from ReflectionTestMetadata
            var testClassField = typeof(ReflectionTestMetadata).GetField("_testClass", BindingFlags.NonPublic | BindingFlags.Instance);
            var testMethodField = typeof(ReflectionTestMetadata).GetField("_testMethod", BindingFlags.NonPublic | BindingFlags.Instance);

            Console.WriteLine($"DEBUG: Checking HasDataSourceAttributes for {reflectionTest.TestName}");
            Console.WriteLine($"DEBUG: testClassField = {testClassField}, testMethodField = {testMethodField}");

            if (testClassField?.GetValue(reflectionTest) is not Type testClass ||
                testMethodField?.GetValue(reflectionTest) is not MethodInfo testMethod)
            {
                Console.WriteLine("DEBUG: Failed to get testClass or testMethod");
                return false;
            }

            Console.WriteLine($"DEBUG: Got testClass = {testClass.Name}, testMethod = {testMethod.Name}");

            // Check for method-level data source attributes
            var methodAttributes = testMethod.GetCustomAttributes().ToList();
            Console.WriteLine($"DEBUG: Method has {methodAttributes.Count} attributes");
            foreach (var attr in methodAttributes)
            {
                Console.WriteLine($"DEBUG: Method attribute: {attr.GetType().Name}");
                if (IsDataSourceAttribute(attr))
                {
                    Console.WriteLine($"DEBUG: Found data source attribute on method: {attr.GetType().Name}");
                    return true;
                }
            }

            // Check for class-level data source attributes
            var classAttributes = testClass.GetCustomAttributes().ToList();
            Console.WriteLine($"DEBUG: Class has {classAttributes.Count} attributes");
            foreach (var attr in classAttributes)
            {
                if (IsDataSourceAttribute(attr))
                {
                    Console.WriteLine($"DEBUG: Found data source attribute on class: {attr.GetType().Name}");
                    return true;
                }
            }

            // Check for property-level data source attributes
            var properties = testClass.GetProperties();
            foreach (var property in properties)
            {
                var propertyAttributes = property.GetCustomAttributes();
                foreach (var attr in propertyAttributes)
                {
                    if (IsDataSourceAttribute(attr))
                    {
                        Console.WriteLine($"DEBUG: Found data source attribute on property {property.Name}: {attr.GetType().Name}");
                        return true;
                    }
                }
            }

            Console.WriteLine("DEBUG: No data source attributes found");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: Exception in HasDataSourceAttributes: {ex.Message}");
            // If we can't determine, assume no data sources
            return false;
        }
    }

    /// <summary>
    /// Checks if an attribute is a data source attribute
    /// </summary>
    private bool IsDataSourceAttribute(Attribute attribute)
    {
        var attributeType = attribute.GetType();
        
        // Check for ArgumentsAttribute
        if (attribute is ArgumentsAttribute)
        {
            return true;
        }

        // Check for MethodDataSourceAttribute
        if (attribute is MethodDataSourceAttribute)
        {
            return true;
        }

        // Check for ClassDataSourceAttribute
        if (attribute is ClassDataSourceAttribute)
        {
            return true;
        }

        // Check for DataSourceGeneratorAttribute<T>
        if (attributeType.BaseType?.IsGenericType == true &&
            attributeType.BaseType?.GetGenericTypeDefinition().Name.Contains("DataSourceGeneratorAttribute") == true)
        {
            return true;
        }

        // Check for AsyncUntypedDataSourceGeneratorAttribute (including MatrixDataSourceAttribute)
        if (attribute is AsyncUntypedDataSourceGeneratorAttribute)
        {
            return true;
        }

        // Check for IAsyncDataSourceGeneratorAttribute
        if (attribute is IAsyncDataSourceGeneratorAttribute)
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// Generic type resolver for source generation mode.
/// In the new approach, generics are resolved during data combination generation,
/// so this resolver allows generic tests to pass through.
/// </summary>
public sealed class SourceGeneratedGenericTypeResolver : IGenericTypeResolver
{
    public Task<IEnumerable<TestMetadata>> ResolveGenericsAsync(IEnumerable<TestMetadata> metadata)
    {
        Console.WriteLine($"SourceGeneratedGenericTypeResolver.ResolveGenericsAsync called with {metadata.Count()} tests");
        
        // In the new approach, generic tests are resolved during data combination generation
        // So we just pass through all tests, including those with generic metadata
        foreach (var test in metadata)
        {
            if (test.GenericTypeInfo != null || test.GenericMethodInfo != null)
            {
                Console.WriteLine($"Found generic test in source generation mode: {test.TestName}. " +
                    $"This will be resolved during data combination generation.");
            }
        }

        return Task.FromResult(metadata);
    }
}
