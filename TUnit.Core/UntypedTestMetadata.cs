using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public record UntypedTestMetadata : TestMetadata
{
    public UntypedTestMetadata([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type testClassType)
    {
        TestClassType = testClassType;

        ResettableLazy = new ResettableLazy<object>(() => Activator.CreateInstance(TestClassType, TestClassArguments)!, string.Empty, new TestBuilderContext());
    }
    
    private ResettableLazy<object> ResettableLazy { get; }
    
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public override Type TestClassType
    {
        get;
    }

    public override TestDetails BuildTestDetails()
    {
        return new UntypedTestDetails(ResettableLazy)
        {
            RepeatLimit = RepeatLimit,
            CurrentRepeatAttempt = CurrentRepeatAttempt,
            TestId = TestId,
            TestName = TestMethod.Name,
            TestMethod = TestMethod,
            TestFilePath = TestFilePath,
            TestLineNumber = TestLineNumber,
            TestClassArguments = TestClassArguments,
            TestMethodArguments = TestMethodArguments,
            TestClassInjectedPropertyArguments = TestClassProperties,
            ReturnType = TestMethod.ReturnType,
            DataAttributes = DynamicAttributes.Where(x => x is IDataAttribute).ToArray()
        };
    }

    internal override DiscoveredTest BuildDiscoveredTest(TestContext testContext)
    {
        return new UntypedDiscoveredTest(ResettableLazy)
        {
            TestContext = testContext,
        };
    }

    public override TestMetadata CloneWithNewMethodFactory(Func<object, CancellationToken, ValueTask> testMethodFactory)
    {
        return new UntypedTestMetadata(TestClassType)
        {
            RepeatLimit = RepeatLimit,
            CurrentRepeatAttempt = CurrentRepeatAttempt,
            TestId = TestId,
            TestMethod = TestMethod,
            TestFilePath = TestFilePath,
            TestLineNumber = TestLineNumber,
            TestClassArguments = TestClassArguments,
            TestMethodArguments = TestMethodArguments,
            DynamicAttributes = DynamicAttributes,
            TestClassProperties = TestClassProperties,
            TestBuilderContext = new TestBuilderContext()
        };
    }
}