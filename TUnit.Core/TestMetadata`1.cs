using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Strongly typed test metadata that provides type-safe test invocation
/// </summary>
/// <typeparam name="T">The type of the test class</typeparam>
public class TestMetadata<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    T> : TestMetadata, ITypedTestMetadata where T : class
{
    private Func<object?[], T>? _instanceFactory;
    private Func<T, object?[], Task>? _testInvoker;

    /// <summary>
    /// Strongly typed instance factory
    /// </summary>
    public new Func<object?[], T>? InstanceFactory
    {
        get => _instanceFactory;
        init
        {
            _instanceFactory = value;
            // Also set the base class property with a wrapper
            if (value != null)
            {
                base.InstanceFactory = args => value(args);
            }
        }
    }

    /// <summary>
    /// Strongly typed test invoker
    /// </summary>
    public new Func<T, object?[], Task>? TestInvoker
    {
        get => _testInvoker;
        init
        {
            _testInvoker = value;
            // Also set the base class property with a wrapper
            if (value != null)
            {
                base.TestInvoker = (instance, args) => value((T)instance, args);
            }
        }
    }

    /// <summary>
    /// Instance creator delegate for unified execution model.
    /// Created at compile-time for AOT mode, at runtime for reflection mode.
    /// </summary>
    public Func<TestContext, Task<object>>? CreateInstance { get; init; }

    /// <summary>
    /// Test invoker delegate with CancellationToken support for unified execution model.
    /// Handles all parameter injection including CancellationToken.
    /// </summary>
    public Func<object, object?[], TestContext, CancellationToken, Task>? InvokeTest { get; init; }

    /// <summary>
    /// Strongly typed test invoker with CancellationToken support.
    /// Used by source generation mode.
    /// </summary>
    public Func<T, object?[], CancellationToken, Task>? InvokeTypedTest { get; init; }


    /// <summary>
    /// Backing field for the DataCombinationGenerator property
    /// </summary>
    private Func<IAsyncEnumerable<TestDataCombination>>? _dataCombinationGenerator;

    /// <summary>
    /// Generator delegate that produces all data combinations for this test.
    /// </summary>
    public override Func<IAsyncEnumerable<TestDataCombination>> DataCombinationGenerator
    {
        get
        {
            if (_dataCombinationGenerator != null)
            {
                return _dataCombinationGenerator;
            }

            // Return empty enumerable if no generator is set
            return () => EmptyAsyncEnumerable();
        }
    }

    #pragma warning disable CS1998 // Async method lacks 'await' operators
    private static async IAsyncEnumerable<TestDataCombination> EmptyAsyncEnumerable()
    {
        yield break;
    }
    #pragma warning restore CS1998

    /// <summary>
    /// Factory delegate that creates an ExecutableTest for this metadata.
    /// </summary>
    public override Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest> CreateExecutableTestFactory
    {
        get
        {
            // Use pre-set delegates if available
            if (CreateInstance != null && InvokeTest != null)
            {
                return (context, metadata) => new UnifiedExecutableTest(
                    (testContext) => CreateInstance(testContext),
                    (instance, args, testContext, cancellationToken) => InvokeTest(instance, args, testContext, cancellationToken))
                {
                    TestId = context.TestId,
                    DisplayName = context.DisplayName,
                    Metadata = metadata,
                    Arguments = context.Arguments,
                    ClassArguments = context.ClassArguments,
                    BeforeTestHooks = context.BeforeTestHooks,
                    AfterTestHooks = context.AfterTestHooks,
                    Context = context.Context
                };
            }

            // For AOT mode, create delegates from the strongly-typed ones
            if (InstanceFactory != null && InvokeTypedTest != null)
            {
                return (context, metadata) =>
                {
                    var typedMetadata = (TestMetadata<T>)metadata;

                    // Create instance delegate that uses context
                    Func<TestContext, Task<object>> createInstance = (testContext) => Task.FromResult<object>(typedMetadata.InstanceFactory!(context.ClassArguments));

                    // Convert InvokeTypedTest to the expected signature
                    Func<object, object?[], TestContext, CancellationToken, Task> invokeTest = async (instance, args, testContext, cancellationToken) =>
                    {
                        await typedMetadata.InvokeTypedTest!((T)instance, args, cancellationToken);
                    };

                    return new UnifiedExecutableTest(createInstance, invokeTest)
                    {
                        TestId = context.TestId,
                        DisplayName = context.DisplayName,
                        Metadata = metadata,
                        Arguments = context.Arguments,
                        ClassArguments = context.ClassArguments,
                            BeforeTestHooks = context.BeforeTestHooks,
                        AfterTestHooks = context.AfterTestHooks,
                        Context = context.Context
                    };
                };
            }

            throw new InvalidOperationException($"Either (CreateInstance and InvokeTest) or (InstanceFactory and InvokeTypedTest) must be set for {typeof(T).Name}");
        }
    }

    /// <summary>
    /// Sets the data combination generator for this test metadata
    /// </summary>
    public void SetDataCombinationGenerator(Func<IAsyncEnumerable<TestDataCombination>> generator)
    {
        _dataCombinationGenerator = generator;
    }
}
