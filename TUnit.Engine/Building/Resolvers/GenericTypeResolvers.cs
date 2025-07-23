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
            catch (Exception ex)
            {
                // For generic test resolution failures, we want the test to fail with a clear error
                // Rather than silently skipping it. The test framework will convert this
                // exception into a failed test result.
                throw new GenericTypeResolutionException(
                    $"Failed to resolve generic test '{test.TestName}': {ex.Message}",
                    ex is GenericTypeResolutionException gtre ? (gtre.InnerException ?? ex) : ex);
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
    [UnconditionalSuppressMessage("Trimming", "IL2067:Target parameter argument does not satisfy DynamicallyAccessedMembersAttribute requirements", Justification = "Reflection mode requires dynamic type access which may not be statically analyzable")]
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
            throw new GenericTypeResolutionException(
                $"Generic test '{genericTest.TestName}' cannot be expanded in reflection mode: " +
                "Test metadata is not of type ReflectionTestMetadata.");
        }

        var testClassField = typeof(ReflectionTestMetadata).GetField("_testClass", BindingFlags.NonPublic | BindingFlags.Instance);
        var testMethodField = typeof(ReflectionTestMetadata).GetField("_testMethod", BindingFlags.NonPublic | BindingFlags.Instance);

        if (testClassField?.GetValue(reflectionMetadata) is not Type testClass ||
            testMethodField?.GetValue(reflectionMetadata) is not MethodInfo testMethod)
        {
            throw new GenericTypeResolutionException(
                $"Generic test '{genericTest.TestName}' cannot be expanded: " +
                "Unable to access test class or method information from metadata.");
        }

        // For now, we'll create a single concrete instance with common types
        // A full implementation would analyze the data to determine the actual types
        var expandedTests = new List<TestMetadata>();

        try
        {

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
                if (hasTypedDataSource)
                {
                    break;
                }
            }

            if (hasTypedDataSource)
            {
                throw new GenericTypeResolutionException(
                    $"Generic test '{genericTest.TestName}' uses typed data sources which cannot be expanded in reflection mode. " +
                    "Typed data sources (DataSourceGeneratorAttribute<T> and AsyncDataSourceGeneratorAttribute<T>) require compile-time type information. " +
                    "Use [Arguments] attributes or non-generic data sources for reflection mode, or use source generation mode.");
            }

            // Get the first data combination to infer types
            var contextAccessor = new TestBuilderContextAccessor(new TestBuilderContext());
            var dataCombinations = genericTest.DataCombinationGenerator(contextAccessor);
            TestDataCombination? firstCombination = null;
            await foreach (var combination in dataCombinations)
            {
                firstCombination = combination;
                break;
            }

            if (firstCombination == null)
            {
                throw new GenericTypeResolutionException(
                    $"Generic test '{genericTest.TestName}' has no data combinations available. " +
                    "Generic tests require at least one data combination to infer type parameters. " +
                    "Ensure your data sources are generating data correctly.");
            }

            // Infer generic type arguments from the data
            var typeArguments = InferTypeArgumentsFromData(testMethod, firstCombination);

            if (typeArguments.Length == 0)
            {
                // Couldn't infer types, return original
                return [genericTest];
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
                TestInvoker = CreateConcreteTestInvoker(testClass, concreteMethod),
                ParameterCount = genericTest.ParameterCount,
                ParameterTypes = concreteMethod.GetParameters().Select(p => p.ParameterType).ToArray(),
                TestMethodParameterTypes = genericTest.TestMethodParameterTypes,
                Hooks = genericTest.Hooks,
                FilePath = genericTest.FilePath,
                LineNumber = genericTest.LineNumber,
                #pragma warning disable IL2067 // Type argument doesn't satisfy DynamicallyAccessedMembers
                MethodMetadata = BuildConcreteMethodMetadata(testClass, concreteMethod, genericTest.MethodMetadata),
                #pragma warning restore IL2067
                GenericTypeInfo = null, // No longer generic
                GenericMethodInfo = null, // No longer generic
                GenericMethodTypeArguments = typeArguments,
                AttributeFactory = genericTest.AttributeFactory,
                PropertyInjections = genericTest.PropertyInjections
            };

            expandedTests.Add(concreteMetadata);
        }
        catch (GenericTypeResolutionException)
        {
            // Re-throw GenericTypeResolutionException as-is
            throw;
        }
        catch (Exception ex)
        {
            // Wrap other exceptions in GenericTypeResolutionException
            throw new GenericTypeResolutionException(
                $"Failed to expand generic test '{genericTest.TestName}': {ex.Message}", ex);
        }

        return expandedTests;
    }

    [RequiresDynamicCode("Type inference requires dynamic code generation")]
    private Type[] InferTypeArgumentsFromData(MethodInfo genericMethod, TestDataCombination dataCombination)
    {
        var genericParams = genericMethod.GetGenericArguments();
        var methodParams = genericMethod.GetParameters();
        var typeArguments = new Type[genericParams.Length];

        // Simple type inference based on parameter positions
        // This assumes generic parameters are used directly as method parameters
        for (int i = 0; i < genericParams.Length; i++)
        {
            var genericParam = genericParams[i];

            // Find which method parameter uses this generic parameter
            for (int j = 0; j < methodParams.Length; j++)
            {
                var paramType = methodParams[j].ParameterType;

                if (paramType == genericParam)
                {
                    // This parameter directly uses the generic type
                    // Get the actual type from the data
                    if (dataCombination.MethodDataFactories != null && j < dataCombination.MethodDataFactories.Length)
                    {
                        var dataTask = dataCombination.MethodDataFactories[j]();
                        var data = dataTask.GetAwaiter().GetResult();

                        if (data != null)
                        {
                            typeArguments[i] = data.GetType();
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
                // Use object as a fallback
                typeArguments[i] = typeof(object);
            }
        }

        return typeArguments;
    }

    /// <summary>
    /// Creates a test invoker for a concrete (non-generic) method
    /// </summary>
    [RequiresDynamicCode("Test invoker creation requires dynamic code generation")]
    private static Func<object, object?[], Task> CreateConcreteTestInvoker(
        Type testClass,
        MethodInfo concreteMethod)
    {
        // For concrete methods, we can create a direct invoker
        return async (instance, args) =>
        {
            try
            {
                var result = concreteMethod.Invoke(
                    concreteMethod.IsStatic ? null : instance,
                    args);

                if (result is Task task)
                {
                    await task;
                }
                else if (result is ValueTask valueTask)
                {
                    await valueTask.AsTask();
                }
            }
            catch (TargetInvocationException tie)
            {
                // Unwrap and rethrow the actual exception
                if (tie.InnerException != null)
                {
                    throw tie.InnerException;
                }
                throw;
            }
        };
    }

    /// <summary>
    /// Builds method metadata for a concrete method based on the original generic method metadata
    /// </summary>
    [RequiresDynamicCode("Method metadata creation requires dynamic code generation")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy DynamicallyAccessedMembersAttribute requirements", Justification = "Reflection mode requires dynamic type access for parameter types")]
    private static MethodMetadata BuildConcreteMethodMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.NonPublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods
            | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClass,
        MethodInfo concreteMethod,
        MethodMetadata originalMetadata)
    {
        return new MethodMetadata
        {
            Name = concreteMethod.Name,
            Type = testClass,
            Class = originalMetadata.Class,
            Parameters = concreteMethod.GetParameters().Select(p =>
            {
                #pragma warning disable IL2072 // Type argument doesn't satisfy DynamicallyAccessedMembers
                var paramMetadata = new ParameterMetadata(p.ParameterType)
                {
                    Name = p.Name ?? "unnamed",
                    TypeReference = TypeReference.CreateConcrete(p.ParameterType.AssemblyQualifiedName!),
                    ReflectionInfo = p
                };
                #pragma warning restore IL2072
                return paramMetadata;
            }).ToArray(),
            GenericTypeCount = 0, // Concrete method has no generic parameters
            ReturnTypeReference = TypeReference.CreateConcrete(concreteMethod.ReturnType.AssemblyQualifiedName!),
            ReturnType = concreteMethod.ReturnType,
            TypeReference = TypeReference.CreateConcrete(testClass.AssemblyQualifiedName!)
        };
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

            if (testClassField?.GetValue(reflectionTest) is not Type testClass ||
                testMethodField?.GetValue(reflectionTest) is not MethodInfo testMethod)
            {
                return false;
            }

            // Check for method-level data source attributes
            var methodAttributes = testMethod.GetCustomAttributes().ToList();
            foreach (var attr in methodAttributes)
            {
                if (IsDataSourceAttribute(attr))
                {
                    return true;
                }
            }

            // Check for class-level data source attributes
            var classAttributes = testClass.GetCustomAttributes().ToList();
            foreach (var attr in classAttributes)
            {
                if (IsDataSourceAttribute(attr))
                {
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
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception)
        {
            // If we can't determine, assume no data sources
            return false;
        }
    }

    /// <summary>
    /// Checks if an attribute is a data source attribute
    /// </summary>
    private bool IsDataSourceAttribute(Attribute attribute)
    {
        return attribute is IDataSourceAttribute;
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
        return Task.FromResult(metadata);
    }
}
