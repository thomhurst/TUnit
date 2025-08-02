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

    /// <summary>
    /// Strongly typed instance factory
    /// </summary>
    public new Func<Type[], object?[], T>? InstanceFactory
    {
        get => _instanceFactory;
        init
        {
            _instanceFactory = value;
            // Also set the base class property with a wrapper
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
            // Also set the base class property with a wrapper
            if (value != null)
            {
                base.TestInvoker = (instance, args) => value((T)instance, args);
            }
        }
    }


    /// <summary>
    /// Strongly typed test invoker with CancellationToken support.
    /// Used by source generation mode.
    /// </summary>
    public Func<T, object?[], CancellationToken, Task>? InvokeTypedTest { get; init; }



    /// <summary>
    /// Factory delegate that creates an ExecutableTest for this metadata.
    /// </summary>
    public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
    {
        get
        {
            // For AOT mode, create delegates from the strongly-typed ones
            if (InstanceFactory != null && InvokeTypedTest != null)
            {
                return (context, metadata) =>
                {
                    var typedMetadata = (TestMetadata<T>)metadata;

                    // Create instance delegate that uses context
                    Func<TestContext, Task<object>> createInstance = async testContext =>
                    {
                        // Try to create instance with ClassConstructor attribute
                        var attributes = metadata.AttributeFactory();
                        var instance = await ClassConstructorHelper.TryCreateInstanceWithClassConstructor(
                            attributes,
                            TestClassType,
                            metadata.TestSessionId,
                            testContext);

                        if (instance != null)
                        {
                            return instance;
                        }

                        // Fall back to default instance factory
                        var typeArgs = testContext.TestDetails.MethodMetadata.Class.Parameters.Select(x => x.Type).ToArray();
                        return typedMetadata.InstanceFactory!(typeArgs, context.ClassArguments);
                    };

                    // Convert InvokeTypedTest to the expected signature
                    Func<object, object?[], TestContext, CancellationToken, Task> invokeTest = async (instance, args, testContext, cancellationToken) =>
                    {
                        await typedMetadata.InvokeTypedTest!((T)instance, args, cancellationToken);
                    };

                    return new ExecutableTest(createInstance, invokeTest)
                    {
                        TestId = context.TestId,
                        Metadata = metadata,
                        Arguments = context.Arguments,
                        ClassArguments = context.ClassArguments,
                        Context = context.Context
                    };
                };
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
