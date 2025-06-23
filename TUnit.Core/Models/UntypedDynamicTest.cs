using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Helpers;

namespace TUnit.Core;

[RequiresDynamicCode("Reflection")]
[RequiresUnreferencedCode("Reflection")]
public record UntypedDynamicTest : DynamicTest
{
    public UntypedDynamicTest(MethodInfo testBody) : this(testBody.ReflectedType ?? testBody.DeclaringType!, testBody)
    {
    }

    public UntypedDynamicTest(Type testClassType, MethodInfo testBody)
    {
        TestId = GetTestId(testBody);
        TestBody = testBody;
        TestClassType = testClassType;
    }

    private static readonly ConcurrentDictionary<string, Counter> DynamicTestCounter = new();

    public override string TestId
    {
        get;
    }

    internal override MethodInfo TestBody
    {
        get;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type TestClassType
    {
        get;
    }

    public TestBuilderContext TestBuilderContext
    {
        get;
        set;
    } = new();

    public override DiscoveryResult BuildTests()
    {
        var testDefinitions = new List<TestDefinition>();
        var discoveryFailures = new List<DiscoveryFailure>();

        try
        {
            var repeatLimit = Attributes.OfType<RepeatAttribute>().FirstOrDefault()?.Times ?? 0;
            var testMethod = BuildTestMethod(TestBody);

            var testDefinition = new TestDefinition
            {
                TestId = TestId,
                MethodMetadata = testMethod,
                TestFilePath = TestFilePath,
                TestLineNumber = TestLineNumber,
                TestClassFactory = () => InstanceHelper.CreateInstance(
                    testMethod,
                    TestClassArguments, Properties, TestBuilderContext),
                TestMethodInvoker = async (instance, token) =>
                {
                    var arguments = TestMethodArguments;

                    if (TestBody.GetParameters().LastOrDefault()?.ParameterType == typeof(CancellationToken))
                    {
                        arguments = TestMethodArguments.Append(token).ToArray();
                    }

                    await AsyncConvert.ConvertObject(TestBody.Invoke(instance, arguments));
                },
                PropertiesProvider = () => Properties ?? new Dictionary<string, object?>(),
                ClassDataProvider = new ArgumentsDataProvider(TestClassArguments ?? Array.Empty<object?>()),
                MethodDataProvider = new ArgumentsDataProvider(TestMethodArguments)
            };

            if (Exception != null)
            {
                discoveryFailures.Add(new DiscoveryFailure
                {
                    TestId = TestId,
                    Exception = Exception,
                    TestFilePath = TestFilePath,
                    TestLineNumber = TestLineNumber,
                    TestClassName = TestClassType.Name,
                    TestMethodName = TestName ?? TestBody.Name
                });
            }
            else
            {
                testDefinitions.Add(testDefinition);
            }
        }
        catch (Exception ex)
        {
            discoveryFailures.Add(new DiscoveryFailure
            {
                TestId = TestId,
                Exception = ex,
                TestFilePath = TestFilePath,
                TestLineNumber = TestLineNumber,
                TestClassName = TestClassType.Name,
                TestMethodName = TestName ?? TestBody.Name
            });
        }

        return new DiscoveryResult
        {
            TestDefinitions = testDefinitions,
            DiscoveryFailures = discoveryFailures
        };
    }


    private static string GetTestId(MethodInfo testBody)
    {
        var typeName = (testBody.ReflectedType ?? testBody.DeclaringType!).FullName;

        var typeAndTestName = typeName + "_" + testBody.Name;

        var count = DynamicTestCounter
            .GetOrAdd(typeAndTestName, _ => new Counter())
            .Increment();

        return $"{typeAndTestName}_{count}";
    }
}
