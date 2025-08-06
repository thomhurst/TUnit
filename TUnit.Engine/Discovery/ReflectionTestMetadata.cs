using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Test metadata implementation that uses reflection for legacy/discovery scenarios
/// </summary>
internal sealed class ReflectionTestMetadata : TestMetadata
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    private readonly Type _testClass;
    private readonly MethodInfo _testMethod;

    public ReflectionTestMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.NonPublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods
            | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClass,
        MethodInfo testMethod)
    {
        _testClass = testClass;
        _testMethod = testMethod;
    }

    [field: AllowNull, MaybeNull]
    public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
    {
        get
        {
            return field ??= CreateExecutableTest;
        }
    }

    private AbstractExecutableTest CreateExecutableTest(ExecutableTestCreationContext context, TestMetadata metadata)
    {
        // Create instance factory that uses reflection
        async Task<object> CreateInstance(TestContext testContext)
        {
            // Try to create instance with ClassConstructor attribute
            var attributes = testContext.TestDetails.Attributes;
            var classConstructorInstance = await ClassConstructorHelper.TryCreateInstanceWithClassConstructor(
                attributes,
                TestClassType,
                metadata.TestSessionId,
                testContext);

            if (classConstructorInstance != null)
            {
                return classConstructorInstance;
            }

            if (InstanceFactory == null)
            {
                throw new InvalidOperationException($"No instance factory for {_testClass.Name}");
            }

            // Get type arguments for generic types
            // For generic types, we need to infer the type arguments from the actual argument values
            Type[] typeArgs;
            if (_testClass.IsGenericTypeDefinition && context.ClassArguments is { Length: > 0 })
            {
                // Infer type arguments from the constructor argument values
                var genericParams = _testClass.GetGenericArguments();
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

            var instance = InstanceFactory(typeArgs, context.ClassArguments ??
            [
            ]);

            // Property injection is handled by SingleTestExecutor after instance creation
            return instance;
        }

        // Create test invoker with CancellationToken support
        // Determine if the test method has a CancellationToken parameter
        var hasCancellationToken = ParameterTypes.Any(t => t == typeof(CancellationToken));
        var cancellationTokenIndex = hasCancellationToken
            ? Array.IndexOf(ParameterTypes, typeof(CancellationToken))
            : -1;

        Func<object, object?[], TestContext, CancellationToken, Task> invokeTest = async (instance, args, testContext, cancellationToken) =>
        {
            if (TestInvoker == null)
            {
                throw new InvalidOperationException($"No test invoker for {_testMethod.Name}");
            }

            if (hasCancellationToken)
            {
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

        return new ExecutableTest(CreateInstance, invokeTest)
        {
            TestId = context.TestId,
            Metadata = metadata,
            Arguments = context.Arguments,
            ClassArguments = context.ClassArguments,
            Context = context.Context
        };
    }
}
