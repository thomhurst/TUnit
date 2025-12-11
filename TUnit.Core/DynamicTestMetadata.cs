using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Unified metadata class for dynamic tests.
/// Used by both AOT/source-generated mode and reflection mode for tests created via
/// DynamicTestBuilderAttribute or runtime test variant creation.
/// </summary>
public sealed class DynamicTestMetadata : TestMetadata, IDynamicTestMetadata
{
    private readonly DynamicDiscoveryResult _dynamicResult;

    public DynamicTestMetadata(DynamicDiscoveryResult dynamicResult)
    {
        _dynamicResult = dynamicResult;
    }

    public int DynamicTestIndex => _dynamicResult.DynamicTestIndex;

    public string? DisplayName => _dynamicResult.DisplayName;

    /// <summary>
    /// Parent test ID for test variants created at runtime.
    /// </summary>
    public string? ParentTestId => _dynamicResult.ParentTestId;

    /// <summary>
    /// Relationship to parent test for test variants.
    /// </summary>
    public Enums.TestRelationship? Relationship => _dynamicResult.Relationship;

    /// <summary>
    /// Custom properties for test variants.
    /// </summary>
    public Dictionary<string, object?>? Properties => _dynamicResult.Properties;

    [field: AllowNull, MaybeNull]
    public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
    {
        get => field ??= CreateExecutableTest;
    }

    private AbstractExecutableTest CreateExecutableTest(ExecutableTestCreationContext context, TestMetadata metadata)
    {
        var modifiedContext = new ExecutableTestCreationContext
        {
            TestId = context.TestId,
            DisplayName = _dynamicResult.DisplayName ?? context.DisplayName,
            Arguments = _dynamicResult.TestMethodArguments ?? context.Arguments,
            ClassArguments = _dynamicResult.TestClassArguments ?? context.ClassArguments,
            Context = context.Context,
            TestClassInstanceFactory = context.TestClassInstanceFactory
        };

        // Apply runtime test variant properties
        if (_dynamicResult.ParentTestId != null)
        {
            modifiedContext.Context.ParentTestId = _dynamicResult.ParentTestId;
        }

        if (_dynamicResult.Relationship.HasValue)
        {
            modifiedContext.Context.Relationship = _dynamicResult.Relationship.Value;
        }

        if (_dynamicResult.Properties != null)
        {
            foreach (var kvp in _dynamicResult.Properties)
            {
                modifiedContext.Context.StateBag.Items[kvp.Key] = kvp.Value;
            }
        }

        // Create instance factory
        var createInstance = async (TestContext testContext) =>
        {
            // If we have a factory from discovery, use it
            if (modifiedContext.TestClassInstanceFactory != null)
            {
                return await modifiedContext.TestClassInstanceFactory();
            }

            // Check if there's a ClassConstructor to use
            if (testContext.ClassConstructor != null)
            {
                var testBuilderContext = TestBuilderContext.FromTestContext(testContext, null);
                var classConstructorMetadata = new ClassConstructorMetadata
                {
                    TestSessionId = metadata.TestSessionId,
                    TestBuilderContext = testBuilderContext
                };

                return await testContext.ClassConstructor.Create(metadata.TestClassType, classConstructorMetadata);
            }

            // Fall back to default instance factory
            var instance = metadata.InstanceFactory(Type.EmptyTypes, modifiedContext.ClassArguments);

            // Handle property injections
            foreach (var propertyInjection in metadata.PropertyInjections)
            {
                var value = propertyInjection.ValueFactory();
                propertyInjection.Setter(instance, value);
            }

            return instance;
        };

        var invokeTest = metadata.TestInvoker ?? throw new InvalidOperationException("Test invoker is null");

        return new ExecutableTest(createInstance,
            async (instance, args, ctx, ct) =>
            {
                await invokeTest(instance, args);
            })
        {
            TestId = modifiedContext.TestId,
            Metadata = metadata,
            Arguments = modifiedContext.Arguments,
            ClassArguments = modifiedContext.ClassArguments,
            Context = modifiedContext.Context
        };
    }
}
