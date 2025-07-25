using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Test metadata implementation for generic types that need runtime type resolution
/// </summary>
public sealed class GenericTestMetadata : TestMetadata
{
    /// <summary>
    /// Factory delegate that creates an ExecutableTest for this metadata.
    /// Uses reflection to handle generic type instantiation and method invocation.
    /// </summary>
    public override Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest> CreateExecutableTestFactory
    {
        get
        {
            return (context, metadata) =>
            {
                // Create instance delegate that uses context
                Func<TestContext, Task<object>> createInstance = async (testContext) =>
                {
                    if (InstanceFactory == null)
                    {
                        throw new InvalidOperationException($"No instance factory for {TestClassType.Name}");
                    }

                    // Get type arguments from test context if generic
                    var typeArgs = testContext.TestDetails.TestClassArguments?.OfType<Type>().ToArray() ?? Type.EmptyTypes;
                    
                    var instance = InstanceFactory(typeArgs, context.ClassArguments);

                    // Apply property values using unified PropertyInjector
                    await PropertyInjector.InjectPropertiesAsync(
                        testContext,
                        instance,
                        PropertyDataSources,
                        PropertyInjections,
                        MethodMetadata,
                        testContext.TestDetails.TestId);

                    return instance;
                };

                // Create test invoker with CancellationToken support
                Func<object, object?[], TestContext, CancellationToken, Task> invokeTest = async (instance, args, testContext, cancellationToken) =>
                {
                    if (TestInvoker == null)
                    {
                        throw new InvalidOperationException($"No test invoker for {TestMethodName}");
                    }

                    // Determine if the test method has a CancellationToken parameter
                    var hasCancellationToken = ParameterTypes.Any(t => t == typeof(CancellationToken));
                    
                    if (hasCancellationToken)
                    {
                        var cancellationTokenIndex = Array.IndexOf(ParameterTypes, typeof(CancellationToken));
                        
                        // Insert CancellationToken at the correct position
                        var argsWithToken = new object?[args.Length + 1];
                        var argIndex = 0;

                        for (var i = 0; i < argsWithToken.Length; i++)
                        {
                            if (i == cancellationTokenIndex)
                            {
                                argsWithToken[i] = cancellationToken;
                            }
                            else if (argIndex < args.Length)
                            {
                                argsWithToken[i] = args[argIndex++];
                            }
                        }

                        await TestInvoker(instance, argsWithToken);
                    }
                    else
                    {
                        await TestInvoker(instance, args);
                    }
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
    }
}