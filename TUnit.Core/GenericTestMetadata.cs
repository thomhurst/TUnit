using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

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
    public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
    {
        get
        {
            return (context, metadata) =>
            {
                Func<TestContext, Task<object>> createInstance = async (testContext) =>
                {
                    // Check for ClassConstructor attribute
                    var attributes = metadata.AttributeFactory();
                    var classConstructorAttribute = attributes.OfType<ClassConstructorAttribute>().FirstOrDefault();

                    if (classConstructorAttribute != null)
                    {
                        // Use the ClassConstructor to create the instance
                        var classConstructorType = classConstructorAttribute.ClassConstructorType;
                        var classConstructor = (IClassConstructor)Activator.CreateInstance(classConstructorType)!;

                        var classConstructorMetadata = new ClassConstructorMetadata
                        {
                            TestSessionId = metadata.TestSessionId,
                            TestBuilderContext = new TestBuilderContext
                            {
                                Events = testContext.Events,
                                ObjectBag = testContext.ObjectBag,
                                TestMetadata = metadata.MethodMetadata
                            }
                        };

                        var classInstance = await classConstructor.Create(TestClassType, classConstructorMetadata);

                        // Apply property values using unified PropertyInjector
                        await PropertyInjector.InjectPropertiesAsync(
                            testContext,
                            classInstance,
                            PropertyDataSources,
                            PropertyInjections,
                            MethodMetadata,
                            testContext.TestDetails.TestId);

                        return classInstance;
                    }

                    // Fall back to default instance factory
                    if (InstanceFactory == null)
                    {
                        throw new InvalidOperationException($"No instance factory for {TestClassType.Name}");
                    }

                    Type[] typeArgs;

                    // First, check if we have resolved class generic arguments from the context
                    // This happens when type inference was done in TestBuilder
                    if (context.ResolvedClassGenericArguments.Length > 0)
                    {
                        typeArgs = context.ResolvedClassGenericArguments;
                    }
                    // Fall back to inferring from constructor arguments if available
                    else if (TestClassType.IsGenericTypeDefinition && context.ClassArguments is { Length: > 0 })
                    {
                        // Infer type arguments from the constructor argument values
                        var genericParams = TestClassType.GetGenericArguments();
                        typeArgs = new Type[genericParams.Length];

                        // For single generic parameter, use the first argument's type
                        if (genericParams.Length == 1 && context.ClassArguments.Length >= 1)
                        {
                            typeArgs[0] = context.ClassArguments[0]?.GetType() ?? typeof(object);
                        }
                        else
                        {
                            // For multiple generic parameters, try to match one-to-one
                            for (var i = 0; i < genericParams.Length; i++)
                            {
                                if (i < context.ClassArguments.Length && context.ClassArguments[i] != null)
                                {
                                    typeArgs[i] = context.ClassArguments[i]!.GetType();
                                }
                                else
                                {
                                    typeArgs[i] = typeof(object);
                                }
                            }
                        }
                    }
                    else
                    {
                        typeArgs = testContext.TestDetails.TestClassArguments?.OfType<Type>().ToArray() ?? Type.EmptyTypes;
                    }

                    var instance = InstanceFactory(typeArgs, context.ClassArguments ?? Array.Empty<object?>());

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
    }
}
