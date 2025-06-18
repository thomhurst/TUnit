using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Enums;

namespace TUnit.Engine.Services.Reflection;

[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("Trimming", "IL2067")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
internal class TestBuilder
{
    public async Task<IEnumerable<DynamicTest>> BuildTestsAsync(
        ClassMetadata classInformation,
        MethodMetadata[] testMethods)
    {
        var dynamicTests = new List<DynamicTest>();

        foreach (var testMethod in testMethods)
        {
            await BuildTestsForMethodAsync(classInformation, testMethod, dynamicTests);
        }
        return dynamicTests;
    }

    private async Task BuildTestsForMethodAsync(
        ClassMetadata classInformation,
        MethodMetadata testMethod,
        List<DynamicTest> dynamicTests)
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
                    await BuildTestVariationsAsync(testMethod, classDataAttribute, testDataAttribute, dynamicTests);
                }
            }
        }
        catch (Exception e)
        {
            AddFailedTest(testMethod, testAttribute, e, classInformation.Type, dynamicTests);
        }
    }

    private async Task BuildTestVariationsAsync(
        MethodMetadata testMethod,
        IDataAttribute classDataAttribute,
        IDataAttribute testDataAttribute,
        List<DynamicTest> dynamicTests)
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
                    testDataAttribute, testBuilderContextAccessor, dynamicTests);
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
        List<DynamicTest> dynamicTests)
    {
        var classInformation = testInformation.Class;
        var testAttribute = testInformation.Attributes.Select(a => a.Instance).OfType<BaseTestAttribute>().First();

        try
        {
            var allAttributes = CollectAllAttributes(testInformation);
            var repeatCount = allAttributes.OfType<RepeatAttribute>().FirstOrDefault()?.Times ?? 0;

            // Prepare the data generator instance once, outside the repeat loop
            var testDataAttributeInstance = await DataGeneratorHandler.PrepareDataGeneratorInstanceAsync(
                testDataAttribute, testInformation, testBuilderContextAccessor);

            for (var index = 0; index < repeatCount + 1; index++)
            {
                await BuildTestInstanceAsync(testInformation, classInstanceArguments, typeDataAttribute,
                    testDataAttributeInstance, testBuilderContextAccessor, allAttributes, dynamicTests);
            }
        }
        catch (Exception e)
        {
            AddFailedTest(testInformation, testAttribute, e, classInformation.Type, dynamicTests);
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
        List<DynamicTest> dynamicTests)
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
            var test = await CreateTestAsync(testInformation, invokedClassInstanceArguments, typeDataAttribute,
                testArguments, testBuilderContextAccessor, allAttributes);

            dynamicTests.Add(test);

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

    private async Task<DynamicTest> CreateTestAsync(
        MethodMetadata testInformation,
        object?[] invokedClassInstanceArguments,
        IDataAttribute typeDataAttribute,
        Func<Task<object?[]>> testArguments,
        TestBuilderContextAccessor testBuilderContextAccessor,
        Attribute[] allAttributes)
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

        return new UntypedDynamicTest(resolvedClassInfo.Type, resolvedMethodInfo.ReflectionInformation)
        {
            TestBuilderContext = testBuilderContextAccessor.Current,
            TestMethodArguments = testMethodArguments,
            Attributes = allAttributes,
            TestName = resolvedMethodInfo.Name,
            TestClassArguments = invokedClassInstanceArguments,
            TestFilePath = testAttribute.File,
            TestLineNumber = testAttribute.Line,
            Properties = propertyArgs,
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
            propertyArgs[propertyInformation.Name] = args.ElementAtOrDefault(0);
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
        List<DynamicTest> dynamicTests)
    {
        dynamicTests.Add(new UntypedFailedDynamicTest(testInformation.ReflectionInformation)
        {
            MethodName = testInformation.Name,
            TestFilePath = testAttribute.File,
            TestLineNumber = testAttribute.Line,
            Exception = exception,
            TestClassType = testClassType,
        });
    }
}
