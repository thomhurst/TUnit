using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;

namespace TUnit.Core;

[RequiresUnreferencedCode("Reflection")]
public record UntypedTestMetadata(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    Type TestClassType)
    : TestMetadata
{
    [field: AllowNull, MaybeNull]
    private ResettableLazy<object> ResettableLazy => field ??= new ResettableLazy<object>(() => InstanceHelper.CreateInstance(TestClassType, TestClassArguments, TestClassProperties), string.Empty, new TestBuilderContext());
    
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public override Type TestClassType
    {
        get;
    } = TestClassType;

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