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
            // If we have a factory from discovery, use it (for lazy instance creation)
            if (context.TestClassInstanceFactory != null)
            {
                return await context.TestClassInstanceFactory();
            }

            // Otherwise fall back to creating instance normally
            // Try to create instance with ClassConstructor attribute
            var attributes = testContext.Metadata.TestDetails.GetAllAttributes();
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

            var typeArgs = context.ResolvedClassGenericArguments;

            var instance = InstanceFactory(typeArgs, context.ClassArguments ??
            [
            ]);

            // Property injection is handled by SingleTestExecutor after instance creation
            return instance;
        }

        // Create test invoker with CancellationToken support. The engine passes the test's token to
        // every CancellationToken parameter, wherever it is declared; row values bind to the others.
        var parameters = MethodMetadata.Parameters;
        var isToken = new bool[parameters.Length];
        var tokenCount = 0;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].Type == typeof(CancellationToken))
            {
                isToken[i] = true;
                tokenCount++;
            }
        }

        // Tokens declared after the last data parameter.
        var trailingTokenCount = 0;
        while (trailingTokenCount < parameters.Length && isToken[parameters.Length - 1 - trailingTokenCount])
        {
            trailingTokenCount++;
        }

        var dataParameterCount = parameters.Length - tokenCount;

        Func<object, object?[], TestContext, CancellationToken, Task> invokeTest = async (instance, args, testContext, cancellationToken) =>
        {
            if (TestInvoker == null)
            {
                throw new InvalidOperationException($"No test invoker for {_testMethod.Name}");
            }

            if (tokenCount == 0)
            {
                await TestInvoker(instance, args);
                return;
            }

            object?[] argsWithToken;
            if (trailingTokenCount > 0 && args.Length > dataParameterCount)
            {
                // More row values than data parameters: a trailing array before the tokens collects
                // them (as in source-gen mode), so keep the values together and the trailing tokens last.
                argsWithToken = new object?[args.Length + tokenCount];
                var argIndex = 0;
                var valuesEnd = argsWithToken.Length - trailingTokenCount;
                for (var i = 0; i < valuesEnd; i++)
                {
                    argsWithToken[i] = i < isToken.Length && isToken[i] && i < parameters.Length - trailingTokenCount
                        ? cancellationToken
                        : args[argIndex++];
                }

                for (var i = valuesEnd; i < argsWithToken.Length; i++)
                {
                    argsWithToken[i] = cancellationToken;
                }
            }
            else
            {
                // Insert the token at each declared position. When the row omits optional parameters
                // before a token, mark those slots Type.Missing so the invoker applies their declared
                // defaults, as the source-generated invoker does.
                var lastTokenIndex = Array.LastIndexOf(isToken, true);
                argsWithToken = new object?[Math.Max(args.Length + tokenCount, lastTokenIndex + 1)];
                var argIndex = 0;

                for (var i = 0; i < argsWithToken.Length; i++)
                {
                    if (i < isToken.Length && isToken[i])
                    {
                        argsWithToken[i] = cancellationToken;
                    }
                    else if (argIndex < args.Length)
                    {
                        argsWithToken[i] = args[argIndex++];
                    }
                    else
                    {
                        argsWithToken[i] = Type.Missing;
                    }
                }
            }

            await TestInvoker(instance, argsWithToken);
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
