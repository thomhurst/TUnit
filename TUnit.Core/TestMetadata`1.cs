using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;
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
    private Func<Type[], object?[], T>? _instanceFactory;
    private Func<T, object?[], Task>? _testInvoker;
    private Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest>? _cachedExecutableTestFactory;

    /// <summary>
    /// Strongly typed instance factory
    /// </summary>
    public new Func<Type[], object?[], T>? InstanceFactory
    {
        get => _instanceFactory;
        init
        {
            _instanceFactory = value;
            if (value != null)
            {
                base.InstanceFactory = (typeArgs, args) => value(typeArgs, args);
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
            if (value != null)
            {
                base.TestInvoker = (instance, args) => value((T)instance, args);
            }
        }
    }


    /// <summary>
    /// Strongly typed test invoker with CancellationToken support.
    /// Used by source generation mode (per-method delegates, generic/inherited tests).
    /// </summary>
    public Func<T, object?[], CancellationToken, ValueTask>? InvokeTypedTest { get; init; }

    /// <summary>
    /// Consolidated class-level invoker that dispatches by method index.
    /// Used by the data-table registration path for non-generic tests.
    /// One switch method per class instead of N separate invoke stubs.
    /// </summary>
    internal Func<T, int, object?[], CancellationToken, ValueTask>? ClassInvoker { get; init; }

    /// <summary>
    /// Method index for <see cref="ClassInvoker"/> dispatch.
    /// </summary>
    internal int InvokeMethodIndex { get; init; } = -1;

    /// <summary>
    /// Consolidated class-level attribute factory that dispatches by group index.
    /// One switch method per class instead of M separate attribute factory methods.
    /// </summary>
    internal Func<int, Attribute[]>? ClassAttributeFactory { get; init; }

    /// <summary>
    /// Attribute group index for <see cref="ClassAttributeFactory"/> dispatch.
    /// </summary>
    internal int AttributeGroupIndex { get; init; } = -1;



    /// <summary>
    /// Factory delegate that creates an ExecutableTest for this metadata.
    /// </summary>
    public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
    {
        get
        {
            // Return cached factory if available
            if (_cachedExecutableTestFactory != null)
            {
                return _cachedExecutableTestFactory;
            }

            // For AOT mode, create delegates from the strongly-typed ones
            var hasPerMethodInvoker = InvokeTypedTest != null;
            var hasClassInvoker = ClassInvoker != null && InvokeMethodIndex >= 0;

            if (InstanceFactory != null && (hasPerMethodInvoker || hasClassInvoker))
            {
                _cachedExecutableTestFactory = (context, metadata) =>
                {
                    var typedMetadata = (TestMetadata<T>)metadata;

                    // Create instance delegate that uses context
                    Func<TestContext, Task<object>> createInstance = async testContext =>
                    {
                        // If we have a factory from discovery, use it (for lazy instance creation)
                        if (context.TestClassInstanceFactory != null)
                        {
                            return await context.TestClassInstanceFactory();
                        }

                        var attributes = metadata.GetOrCreateAttributes();
                        var instance = await ClassConstructorHelper.TryCreateInstanceWithClassConstructor(
                            attributes,
                            TestClassType,
                            metadata.TestSessionId,
                            testContext);

                        if (instance != null)
                        {
                            return instance;
                        }

                        return typedMetadata.InstanceFactory!(context.ResolvedClassGenericArguments, context.ClassArguments);
                    };

                    // Use class-level consolidated invoker when available (index-based dispatch,
                    // 1 JIT per class instead of N per method). Fall back to per-method delegate.
                    Func<object, object?[], TestContext, CancellationToken, Task> invokeTest;
                    if (typedMetadata.ClassInvoker != null && typedMetadata.InvokeMethodIndex >= 0)
                    {
                        var classInvoker = typedMetadata.ClassInvoker;
                        var methodIndex = typedMetadata.InvokeMethodIndex;
                        invokeTest = async (instance, args, testContext, cancellationToken) =>
                        {
                            await classInvoker((T)instance, methodIndex, args, cancellationToken).ConfigureAwait(false);
                        };
                    }
                    else
                    {
                        invokeTest = async (instance, args, testContext, cancellationToken) =>
                        {
                            await typedMetadata.InvokeTypedTest!((T)instance, args, cancellationToken).ConfigureAwait(false);
                        };
                    }

                    return new ExecutableTest(createInstance, invokeTest)
                    {
                        TestId = context.TestId,
                        Metadata = metadata,
                        Arguments = context.Arguments,
                        ClassArguments = context.ClassArguments,
                        Context = context.Context
                    };
                };

                return _cachedExecutableTestFactory;
            }

            throw new InvalidOperationException($"InstanceFactory and InvokeTypedTest must be set for {typeof(T).Name}");
        }
    }

    /// <summary>
    /// Sets up runtime data combination generation for this test by updating the TestSessionId
    /// </summary>
    public void UseRuntimeDataGeneration(string testSessionId)
    {
        TestSessionId = testSessionId;
    }
}
