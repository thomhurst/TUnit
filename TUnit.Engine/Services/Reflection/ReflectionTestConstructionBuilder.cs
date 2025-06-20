using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services.Reflection;

[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("Trimming", "IL2067")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
internal class ReflectionTestConstructionBuilder
{
    public async Task<DiscoveryResult> BuildTestsAsync(
        ClassMetadata classInformation,
        MethodMetadata[] testMethods)
    {
        var testDefinitions = new List<ITestDefinition>();
        var discoveryFailures = new List<DiscoveryFailure>();

        foreach (var testMethod in testMethods)
        {
            await BuildTestsForMethodAsync(classInformation, testMethod, testDefinitions, discoveryFailures);
        }

        return new DiscoveryResult
        {
            TestDefinitions = testDefinitions,
            DiscoveryFailures = discoveryFailures
        };
    }

    private async Task BuildTestsForMethodAsync(
        ClassMetadata classInformation,
        MethodMetadata testMethod,
        List<ITestDefinition> testDefinitions,
        List<DiscoveryFailure> discoveryFailures)
    {
        var testAttribute = testMethod.Attributes.Select(a => a.Instance).OfType<TestAttribute>().First();

        try
        {
            var classDataAttributes = GetDataAttributes(classInformation);
            var testDataAttributes = GetDataAttributes(testMethod);

            foreach (var classDataAttribute in classDataAttributes)
            {
                foreach (var testDataAttribute in testDataAttributes)
                {
                    await BuildTestVariationsAsync(testMethod, classDataAttribute, testDataAttribute, testDefinitions, discoveryFailures);
                }
            }
        }
        catch (Exception e)
        {
            AddFailedTest(testMethod, testAttribute, e, classInformation.Type, discoveryFailures);
        }
    }

    private async Task BuildTestVariationsAsync(
        MethodMetadata testMethod,
        IDataAttribute classDataAttribute,
        IDataAttribute testDataAttribute,
        List<ITestDefinition> testDefinitions,
        List<DiscoveryFailure> discoveryFailures)
    {
        var testBuilderContext = new TestBuilderContext
        {
            TestMethodName = testMethod.Name,
            ClassInformation = testMethod.Class,
            MethodInformation = testMethod
        };
        var testBuilderContextAccessor = new TestBuilderContextAccessor(testBuilderContext);

        // Set the AsyncLocal for data generators to access
        var previousContext = TestBuilderContext.Current;
        TestBuilderContext.Current = testBuilderContext;

        try
        {
            var classArgumentsContext = new DataGeneratorContext
            {
                TypeDataAttribute = classDataAttribute,
                ClassInformation = testMethod.Class,
                Method = testMethod,
                PropertyInfo = null,
                TestDataAttribute = classDataAttribute,
                DataGeneratorType = DataGeneratorType.ClassParameters,
                ClassInstanceArguments = () => [],
                TestInformation = testMethod,
                TestBuilderContextAccessor = testBuilderContextAccessor,
                ClassInstanceArgumentsInvoked = null,
                NeedsInstance = false
            };

            await foreach (var classInstanceArguments in DataGeneratorHandler.GetArgumentsFromDataAttributeAsync(
                classDataAttribute, classArgumentsContext))
            {
                await BuildSingleTestAsync(testMethod, classInstanceArguments, classDataAttribute,
                    testDataAttribute, testBuilderContextAccessor, testDefinitions, discoveryFailures);
            }
        }
        finally
        {
            // Restore previous context
            TestBuilderContext.Current = previousContext;
        }
    }

    private async Task BuildSingleTestAsync(
        MethodMetadata testInformation,
        Func<Task<object?[]>> classInstanceArguments,
        IDataAttribute typeDataAttribute,
        IDataAttribute testDataAttribute,
        TestBuilderContextAccessor testBuilderContextAccessor,
        List<ITestDefinition> testDefinitions,
        List<DiscoveryFailure> discoveryFailures)
    {
        var classInformation = testInformation.Class;
        var testAttribute = testInformation.Attributes.Select(a => a.Instance).OfType<BaseTestAttribute>().First();

        try
        {
            var allAttributes = CollectAllAttributes(testInformation);

            var testDataAttributeInstance = await DataGeneratorHandler.PrepareDataGeneratorInstanceAsync(
                testDataAttribute, testInformation, testBuilderContextAccessor);

            await BuildTestInstanceAsync(testInformation, classInstanceArguments, typeDataAttribute,
                testDataAttributeInstance, testBuilderContextAccessor, allAttributes, 0, testDefinitions, discoveryFailures);
        }
        catch (Exception e)
        {
            AddFailedTest(testInformation, testAttribute, e, classInformation.Type, discoveryFailures);
            var newContext = new TestBuilderContext
            {
                TestMethodName = testInformation.Name,
                ClassInformation = testInformation.Class,
                MethodInformation = testInformation
            };
            testBuilderContextAccessor.Current = newContext;
            TestBuilderContext.Current = newContext;
        }
    }

    private async Task BuildTestInstanceAsync(
        MethodMetadata testInformation,
        Func<Task<object?[]>> classInstanceArguments,
        IDataAttribute typeDataAttribute,
        IDataAttribute testDataAttributeInstance,
        TestBuilderContextAccessor testBuilderContextAccessor,
        Attribute[] allAttributes,
        int currentRepeatAttempt,
        List<ITestDefinition> testDefinitions,
        List<DiscoveryFailure> discoveryFailures)
    {
        var invokedClassInstanceArguments = await classInstanceArguments();

        var testArgumentsContext = new DataGeneratorContext
        {
            TypeDataAttribute = typeDataAttribute,
            ClassInformation = testInformation.Class,
            Method = testInformation,
            PropertyInfo = null,
            TestDataAttribute = testDataAttributeInstance,
            DataGeneratorType = DataGeneratorType.TestParameters,
            ClassInstanceArguments = () => invokedClassInstanceArguments,
            TestInformation = testInformation,
            TestBuilderContextAccessor = testBuilderContextAccessor,
            ClassInstanceArgumentsInvoked = invokedClassInstanceArguments,
            NeedsInstance = testInformation.Parameters.SelectMany(x => x.Attributes)
                .Any(x => x is IAccessesInstanceData)
        };

        await foreach (var testArguments in DataGeneratorHandler.GetArgumentsFromDataAttributeAsync(
            testDataAttributeInstance, testArgumentsContext))
        {
            var testDefinition = await CreateTestDefinitionAsync(testInformation, invokedClassInstanceArguments, typeDataAttribute,
                testArguments, testBuilderContextAccessor, allAttributes, currentRepeatAttempt);

            testDefinitions.Add(testDefinition);

            var newContext = new TestBuilderContext
            {
                TestMethodName = testInformation.Name,
                ClassInformation = testInformation.Class,
                MethodInformation = testInformation
            };
            testBuilderContextAccessor.Current = newContext;
            TestBuilderContext.Current = newContext;
            invokedClassInstanceArguments = await classInstanceArguments();
        }
    }

    private async Task<TestDefinition> CreateTestDefinitionAsync(
        MethodMetadata testInformation,
        object?[] invokedClassInstanceArguments,
        IDataAttribute typeDataAttribute,
        Func<Task<object?[]>> testArguments,
        TestBuilderContextAccessor testBuilderContextAccessor,
        Attribute[] allAttributes,
        int currentRepeatAttempt)
    {
        var classInformation = testInformation.Class;
        var testAttribute = testInformation.Attributes.Select(a => a.Instance).OfType<BaseTestAttribute>().First();

        var propertyArgs = await GetPropertyArgumentsAsync(classInformation, invokedClassInstanceArguments,
            testInformation, testBuilderContextAccessor);

        if (typeDataAttribute is not ClassConstructorAttribute)
        {
            ParameterMapper.MapImplicitParameters(ref invokedClassInstanceArguments,
                testInformation.Class.Parameters);
        }

        var testMethodArguments = await testArguments();
        ParameterMapper.MapImplicitParameters(ref testMethodArguments, testInformation.Parameters);

        // Handle generic types
        var (resolvedClassInfo, resolvedMethodInfo) = GenericTypeResolver.ResolveGenericClass(
            classInformation, testInformation, invokedClassInstanceArguments);

        if (resolvedMethodInfo.ReflectionInformation.ContainsGenericParameters)
        {
            resolvedMethodInfo = GenericTypeResolver.ResolveGenericMethod(
                resolvedMethodInfo, testMethodArguments);
        }

        var classType = resolvedClassInfo.Type;
        var methodInfo = resolvedMethodInfo.ReflectionInformation;

        // Generate test ID
        var testId = TestIdGenerator.GenerateTestId(
            classType,
            methodInfo.Name,
            invokedClassInstanceArguments,
            testMethodArguments,
            currentRepeatAttempt
        );

        return new TestDefinition
        {
            TestId = testId,
            MethodMetadata = resolvedMethodInfo,
            TestFilePath = testAttribute.File,
            TestLineNumber = testAttribute.Line,
            TestClassFactory = () => Activator.CreateInstance(classType, invokedClassInstanceArguments)!,
            TestMethodInvoker = (instance, cancellationToken) =>
            {
                var result = methodInfo.Invoke(instance, testMethodArguments);
                if (result is Task task)
                {
                    return new ValueTask(task);
                }
                return new ValueTask();
            },
            ClassArgumentsProvider = () => invokedClassInstanceArguments,
            MethodArgumentsProvider = () => testMethodArguments,
            PropertiesProvider = () => propertyArgs
        };
    }

    private async Task<Dictionary<string, object?>> GetPropertyArgumentsAsync(
        ClassMetadata classInformation,
        object?[] classInstanceArguments,
        MethodMetadata testInformation,
        TestBuilderContextAccessor testBuilderContextAccessor)
    {
        var propertyArgs = new Dictionary<string, object?>();

        await foreach (var (propertyInformation, argsFunc) in InstanceCreator.GetPropertyArgumentsEnumerableAsync(
            classInformation, classInstanceArguments, testInformation, testBuilderContextAccessor))
        {
            var args = await argsFunc();
            var propertyValue = args.ElementAtOrDefault(0);

            // If this is an IAsyncDataSourceGeneratorAttribute property and it implements IAsyncInitializer,
            // we need to initialize it immediately during discovery
            var dataAttribute = propertyInformation.Attributes
                .OfType<IDataAttribute>()
                .FirstOrDefault();

            if (dataAttribute is IAsyncDataSourceGeneratorAttribute && propertyValue is IAsyncInitializer)
            {
                await ObjectInitializer.InitializeAsync(propertyValue);
            }

            propertyArgs[propertyInformation.Name] = propertyValue;
        }

        return propertyArgs;
    }

    private static Attribute[] CollectAllAttributes(MethodMetadata testInformation)
    {
        return
        [
            ..testInformation.Attributes.Select(a => a.Instance),
            ..testInformation.Class.Attributes.Select(a => a.Instance),
            ..testInformation.Class.Assembly.Attributes.Select(a => a.Instance)
        ];
    }

    private static IEnumerable<IDataAttribute> GetDataAttributes(MemberMetadata memberInformation)
    {
        var attributes = memberInformation.Attributes.Select(a => a.Instance).OfType<IDataAttribute>().ToArray();
        return attributes.Length == 0 ? NoOpDataAttribute.Array : attributes;
    }

    private void AddFailedTest(
        MethodMetadata testInformation,
        BaseTestAttribute testAttribute,
        Exception exception,
        Type testClassType,
        List<DiscoveryFailure> discoveryFailures)
    {
        var currentRepeatAttempt = 0;

        var testId = TestIdGenerator.GenerateTestId(
            testClassType,
            testInformation.Name,
            Array.Empty<object?>(),
            Array.Empty<object?>(),
            currentRepeatAttempt
        );

        discoveryFailures.Add(new DiscoveryFailure
        {
            TestId = testId,
            Exception = exception,
            TestFilePath = testAttribute.File,
            TestLineNumber = testAttribute.Line,
            TestClassName = testClassType.Name,
            TestMethodName = testInformation.Name
        });
    }
}
