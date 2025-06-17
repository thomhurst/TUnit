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

    public override IEnumerable<TestConstructionData> BuildTestConstructionData()
    {
        var repeatLimit = Attributes.OfType<RepeatAttribute>().FirstOrDefault()?.Times ?? 0;
        var testMethod = BuildTestMethod(TestBody);
        
        for (var i = 0; i <= repeatLimit; i++)
        {
            yield return new TestConstructionData
            {
                TestId = $"{TestId}-{i}",
                TestMethod = testMethod,
                RepeatCount = repeatLimit + 1,
                CurrentRepeatAttempt = i,
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
                ClassArgumentsProvider = () => TestClassArguments ?? [],
                MethodArgumentsProvider = () => TestMethodArguments,
                PropertiesProvider = () => Properties ?? new Dictionary<string, object?>(),
                TestBuilderContext = TestBuilderContext,
                DiscoveryException = Exception
            };
        }
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
