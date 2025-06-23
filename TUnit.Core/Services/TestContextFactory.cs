using TUnit.Core.Interfaces;
using TUnit.Core.Extensions;

namespace TUnit.Core.Services;

/// <summary>
/// Default implementation of test context factory.
/// </summary>
public class TestContextFactory : ITestContextFactory
{
    private readonly IServiceProvider _serviceProvider;

    public TestContextFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public TestContext CreateContext(
        TestDetails testDetails,
        object? classInstance,
        object?[]? classArguments,
        object?[]? methodArguments)
    {
        // Create a test builder context for this test
        var testBuilderContext = new TestBuilderContext
        {
            ClassInformation = testDetails.ClassMetadata,
            MethodInformation = testDetails.MethodMetadata,
            TestMethodName = testDetails.MethodMetadata.Name
        };

        // Create assembly hook context
        var assemblyHookContext = AssemblyHookContext.Current ?? 
            new AssemblyHookContext(TestSessionContext.Current!) 
            { 
                Assembly = testDetails.MethodMetadata.Class.Type.Assembly 
            };
        
        // Create class hook context
        var classHookContext = new ClassHookContext(assemblyHookContext)
        {
            ClassType = testDetails.MethodMetadata.Class.Type
        };

        // Create the test context with proper initialization
        var testContext = new TestContext(
            _serviceProvider,
            testDetails,
            null, // No original test definition available in this context
            testBuilderContext,
            classHookContext);

        return testContext;
    }

    /// <inheritdoc />
    public async Task<TestContext> CreateTestContextAsync(ExpandedTest expandedTest)
    {
        // Create test details from the expanded test
        var testDetails = CreateTestDetails(expandedTest);

        // Create the test context
        var testContext = CreateContext(
            testDetails,
            expandedTest.TestInstance,
            expandedTest.ClassArguments,
            expandedTest.MethodArguments);

        return await Task.FromResult(testContext);
    }

    /// <inheritdoc />
    public TestDetails CreateTestDetails(TestDefinition definition, string testName)
    {
        // Create a test builder context for this test
        var testBuilderContext = new TestBuilderContext
        {
            ClassInformation = definition.MethodMetadata.Class,
            MethodInformation = definition.MethodMetadata,
            TestMethodName = definition.MethodMetadata.Name
        };
        // Create a resettable lazy for the class instance
        var lazyInstance = new ResettableLazy<object>(
            definition.TestClassFactory,
            definition.TestId,
            testBuilderContext);

        // Create test details using the non-generic version
        var testDetails = new TestDetails<object>
        {
            TestId = definition.TestId,
            TestName = testName,
            LazyClassInstance = lazyInstance,
            TestClassArguments = Array.Empty<object?>(),
            TestMethodArguments = Array.Empty<object?>(),
            TestClassInjectedPropertyArguments = definition.PropertiesProvider(),
            MethodMetadata = definition.MethodMetadata,
            // ClassMetadata is a computed property, no need to set it
            ReturnType = definition.MethodMetadata.ReturnType ?? typeof(void),
            TestFilePath = definition.TestFilePath,
            TestLineNumber = definition.TestLineNumber,
            DataAttributes = Array.Empty<AttributeMetadata>(),
            DynamicAttributes = Array.Empty<AttributeMetadata>()
        };

        // Add categories from attributes
        var categoryAttributes = definition.MethodMetadata.GetAttributes<CategoryAttribute>();
        foreach (var categoryAttribute in categoryAttributes)
        {
            testDetails.MutableCategories.Add(categoryAttribute.Category);
        }

        // Set retry limit from attributes
        var retryAttribute = definition.MethodMetadata.GetAttribute<RetryAttribute>();
        if (retryAttribute != null)
        {
            testDetails.RetryLimit = retryAttribute.Times;
        }

        return testDetails;
    }

    private TestDetails CreateTestDetails(ExpandedTest expandedTest)
    {
        // Create a resettable lazy for the class instance
        // Create a test builder context for this test
        var testBuilderContext = new TestBuilderContext
        {
            ClassInformation = expandedTest.MethodMetadata.Class,
            MethodInformation = expandedTest.MethodMetadata,
            TestMethodName = expandedTest.MethodMetadata.Name
        };
        
        var lazyInstance = new ResettableLazy<object>(
            () => expandedTest.TestInstance,
            expandedTest.TestId,
            testBuilderContext);

        // Create test details using the non-generic version
        var testDetails = new TestDetails<object>
        {
            TestId = expandedTest.TestId,
            TestName = expandedTest.TestName,
            LazyClassInstance = lazyInstance,
            TestClassArguments = expandedTest.ClassArguments ?? Array.Empty<object?>(),
            TestMethodArguments = expandedTest.MethodArguments ?? Array.Empty<object?>(),
            TestClassInjectedPropertyArguments = expandedTest.PropertyValues ?? new Dictionary<string, object?>(),
            MethodMetadata = expandedTest.MethodMetadata,
            // ClassMetadata is a computed property, no need to set it
            ReturnType = expandedTest.MethodMetadata.ReturnType ?? typeof(void),
            TestFilePath = expandedTest.TestFilePath,
            TestLineNumber = expandedTest.TestLineNumber,
            DataAttributes = Array.Empty<AttributeMetadata>(),
            DynamicAttributes = Array.Empty<AttributeMetadata>()
        };

        // Set timeout if specified
        if (expandedTest.Timeout.HasValue)
        {
            testDetails.Timeout = expandedTest.Timeout.Value;
        }

        // Add categories from attributes
        var categoryAttributes = expandedTest.MethodMetadata.GetAttributes<CategoryAttribute>();
        foreach (var categoryAttribute in categoryAttributes)
        {
            testDetails.MutableCategories.Add(categoryAttribute.Category);
        }

        // Set retry limit from attributes
        var retryAttribute = expandedTest.MethodMetadata.GetAttribute<RetryAttribute>();
        if (retryAttribute != null)
        {
            testDetails.RetryLimit = retryAttribute.Times;
        }

        return testDetails;
    }
}