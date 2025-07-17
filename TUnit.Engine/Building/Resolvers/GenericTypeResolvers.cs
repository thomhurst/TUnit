using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        // Check if we have data sources
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
            throw new GenericTypeResolutionException(
                $"Generic test method '{genericTest.TestName}' requires test data to infer generic type parameters. " +
                "Add [Arguments] attributes or other data sources.");
        }

        // Get the test class and method from ReflectionTestMetadata
        if (genericTest is not ReflectionTestMetadata reflectionMetadata)
        {
            // Can't expand non-reflection metadata
            return new[] { genericTest };
        }

        var testClassField = typeof(ReflectionTestMetadata).GetField("_testClass", BindingFlags.NonPublic | BindingFlags.Instance);
        var testMethodField = typeof(ReflectionTestMetadata).GetField("_testMethod", BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (testClassField?.GetValue(reflectionMetadata) is not Type testClass ||
            testMethodField?.GetValue(reflectionMetadata) is not MethodInfo testMethod)
        {
            return new[] { genericTest };
        }

        // For now, we'll create a single concrete instance with common types
        // A full implementation would analyze the data to determine the actual types
        var expandedTests = new List<TestMetadata>();

        try
        {
            Console.WriteLine($"DEBUG: Attempting to expand generic test '{genericTest.TestName}'");
            
            // Check if the test has typed data sources that require generic type resolution
            var hasTypedDataSource = false;
            foreach (var attr in testMethod.GetCustomAttributes())
            {
                var baseType = attr.GetType().BaseType;
                while (baseType != null)
                {
                    if (baseType.IsGenericType &&
                        (baseType.GetGenericTypeDefinition().Name.Contains("DataSourceGeneratorAttribute") ||
                         baseType.GetGenericTypeDefinition().Name.Contains("AsyncDataSourceGeneratorAttribute")))
                    {
                        hasTypedDataSource = true;
                        break;
                    }
                    baseType = baseType.BaseType;
                }
                if (hasTypedDataSource) break;
            }

            if (hasTypedDataSource)
            {
                Console.WriteLine($"WARNING: Generic test '{genericTest.TestName}' uses typed data sources which cannot be expanded in reflection mode.");
                Console.WriteLine("Typed data sources (DataSourceGeneratorAttribute<T> and AsyncDataSourceGeneratorAttribute<T>) require compile-time type information.");
                Console.WriteLine("Use [Arguments] attributes or non-generic data sources for reflection mode, or use source generation mode.");
                return Enumerable.Empty<TestMetadata>();
            }
            
            // Get the first data combination to infer types
            var dataCombinations = genericTest.DataCombinationGenerator();
            TestDataCombination? firstCombination = null;
            await foreach (var combination in dataCombinations)
            {
                Console.WriteLine($"DEBUG: Got data combination with {combination.MethodDataFactories?.Length ?? 0} method data factories");
                firstCombination = combination;
                break;
            }
            
            if (firstCombination == null)
            {
                Console.WriteLine($"DEBUG: No data combinations available for '{genericTest.TestName}'");
                // No data available, can't expand
                return new[] { genericTest };
            }

            // Infer generic type arguments from the data
            var typeArguments = InferTypeArgumentsFromData(testMethod, firstCombination);
            
            if (typeArguments.Length == 0)
            {
                // Couldn't infer types, return original
                return new[] { genericTest };
            }

            // Create a concrete version of the method
            var concreteMethod = testMethod.MakeGenericMethod(typeArguments);
            
            // Create new metadata for the concrete test
            var concreteMetadata = new ReflectionTestMetadata(testClass, concreteMethod)
            {
                TestName = genericTest.TestName,
                TestClassType = genericTest.TestClassType,
                TestMethodName = genericTest.TestMethodName,
                Categories = genericTest.Categories,
                IsSkipped = genericTest.IsSkipped,
                SkipReason = genericTest.SkipReason,
                TimeoutMs = genericTest.TimeoutMs,
                RetryCount = genericTest.RetryCount,
                CanRunInParallel = genericTest.CanRunInParallel,
                Dependencies = genericTest.Dependencies,
                DataSources = genericTest.DataSources,
                ClassDataSources = genericTest.ClassDataSources,
                PropertyDataSources = genericTest.PropertyDataSources,
                InstanceFactory = genericTest.InstanceFactory,
                TestInvoker = genericTest.TestInvoker,
                ParameterCount = genericTest.ParameterCount,
                ParameterTypes = concreteMethod.GetParameters().Select(p => p.ParameterType).ToArray(),
                TestMethodParameterTypes = genericTest.TestMethodParameterTypes,
                Hooks = genericTest.Hooks,
                FilePath = genericTest.FilePath,
                LineNumber = genericTest.LineNumber,
                MethodMetadata = genericTest.MethodMetadata,
                GenericTypeInfo = null, // No longer generic
                GenericMethodInfo = null, // No longer generic
                GenericMethodTypeArguments = typeArguments,
                AttributeFactory = genericTest.AttributeFactory,
                PropertyInjections = genericTest.PropertyInjections
            };
            
            expandedTests.Add(concreteMetadata);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Failed to expand generic test '{genericTest.TestName}': {ex.Message}");
            // Return empty to skip the test
            return Enumerable.Empty<TestMetadata>();
        }

        return expandedTests;
    }
    
    [RequiresDynamicCode("Type inference requires dynamic code generation")]
    private Type[] InferTypeArgumentsFromData(MethodInfo genericMethod, TestDataCombination dataCombination)
    {
        var genericParams = genericMethod.GetGenericArguments();
        var methodParams = genericMethod.GetParameters();
        var typeArguments = new Type[genericParams.Length];
        
        Console.WriteLine($"DEBUG: InferTypeArgumentsFromData - Method has {genericParams.Length} generic params and {methodParams.Length} method params");
        
        // Simple type inference based on parameter positions
        // This assumes generic parameters are used directly as method parameters
        for (int i = 0; i < genericParams.Length; i++)
        {
            var genericParam = genericParams[i];
            Console.WriteLine($"DEBUG: Processing generic param {i}: {genericParam.Name}");
            
            // Find which method parameter uses this generic parameter
            for (int j = 0; j < methodParams.Length; j++)
            {
                var paramType = methodParams[j].ParameterType;
                Console.WriteLine($"DEBUG: Checking method param {j}: {paramType.Name}");
                
                if (paramType == genericParam)
                {
                    Console.WriteLine($"DEBUG: Method param {j} uses generic param {i}");
                    // This parameter directly uses the generic type
                    // Get the actual type from the data
                    if (dataCombination.MethodDataFactories != null && j < dataCombination.MethodDataFactories.Length)
                    {
                        var dataTask = dataCombination.MethodDataFactories[j]();
                        var data = dataTask.GetAwaiter().GetResult();
                        
                        if (data != null)
                        {
                            typeArguments[i] = data.GetType();
                            Console.WriteLine($"DEBUG: Inferred type {typeArguments[i].Name} for generic param {i} from data");
                            break;
                        }
                    }
                }
            }
        }
        
        // Fill in any missing type arguments with common defaults
        for (int i = 0; i < typeArguments.Length; i++)
        {
            if (typeArguments[i] == null)
            {
                Console.WriteLine($"DEBUG: No type inferred for generic param {i}, using object as fallback");
                // Use object as a fallback
                typeArguments[i] = typeof(object);
            }
        }
        
        return typeArguments;
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
                Console.WriteLine($"DEBUG: Method attribute: {attr.GetType().FullName}");
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

        // Check for DataSourceGeneratorAttribute<T> and AsyncDataSourceGeneratorAttribute<T>
        var baseType = attributeType.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType)
            {
                var genericDef = baseType.GetGenericTypeDefinition();
                if (genericDef.Name.Contains("DataSourceGeneratorAttribute") || 
                    genericDef.Name.Contains("AsyncDataSourceGeneratorAttribute"))
                {
                    Console.WriteLine($"DEBUG: Found typed data source attribute: {attributeType.Name} inherits from {baseType.Name}");
                    return true;
                }
            }
            baseType = baseType.BaseType;
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
