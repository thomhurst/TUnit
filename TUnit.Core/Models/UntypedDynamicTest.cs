using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Helpers;

namespace TUnit.Core;

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
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type TestClassType
    {
        get;
    }

    public override IEnumerable<TestMetadata> BuildTestMetadatas()
    {
        yield return new UntypedTestMetadata(TestClassType)
        {
            TestId = TestId,
            TestMethod = BuildTestMethod(TestBody),
            CurrentRepeatAttempt = 0,
            RepeatLimit = Attributes.OfType<RepeatAttribute>().FirstOrDefault()?.Times ?? 0,
            TestBuilderContext = new TestBuilderContext(),
            TestClassArguments = TestClassArguments ?? [],
            TestClassProperties = Properties?.Values.ToArray() ?? [],
            TestFilePath = TestFilePath,
            TestLineNumber = TestLineNumber,
            TestMethodArguments = TestMethodArguments,
            DynamicAttributes = Attributes,
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