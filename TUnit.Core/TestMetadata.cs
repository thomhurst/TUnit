using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

public record TestMetadata<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TClassType> : TestMetadata where TClassType : class
{
    public required ResettableLazy<TClassType> ResettableClassFactory { get; init; }
    public required Func<TClassType, CancellationToken, Task> TestMethodFactory { get; init; }
    
    public override TestDetails BuildTestDetails()
    {
		var testId = TestId;
		
		var testDetails = new TestDetails<TClassType>
		{
			TestId = testId,
			LazyClassInstance = ResettableClassFactory,
			TestClassArguments = TestClassArguments,
			TestMethodArguments = TestMethodArguments,
			TestClassInjectedPropertyArguments = TestClassProperties,
			CurrentRepeatAttempt = CurrentRepeatAttempt,
			RepeatLimit = RepeatLimit,
			TestMethod = TestMethod,
			TestName = TestMethod.Name,
			ReturnType = TestMethod.ReturnType,
			TestFilePath = TestFilePath,
			TestLineNumber = TestLineNumber,
		};

		return testDetails;
    }

    internal override DiscoveredTest BuildDiscoveredTest(TestContext testContext)
    {
	    return new DiscoveredTest<TClassType>(ResettableClassFactory)
	    {
		    TestContext = testContext,
		    TestBody = (classInstance, cancellationToken) => TestMethodFactory(classInstance, cancellationToken),
	    };
    }

    public override TestMetadata CloneWithNewMethodFactory(Func<object, CancellationToken, Task> testMethodFactory)
    {
	    return this with
	    {
		    TestMethodFactory = testMethodFactory.Invoke,
		    ResettableClassFactory = ResettableClassFactory.Clone()
	    };
    }
}

public abstract record TestMetadata
{
    public required string TestId { get; init; }
    
    public required SourceGeneratedMethodInformation TestMethod { get; init; }
    
    public required int RepeatLimit { get; init; }
    public required int CurrentRepeatAttempt { get; init; }
    
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }
    
    public required object?[] TestClassArguments { get; init; }
    public required object?[] TestMethodArguments { get; init; }
    public required object?[] TestClassProperties { get; init; }
    
    public required TestBuilderContext TestBuilderContext { get; init; }
    
    public abstract TestDetails BuildTestDetails();
    internal abstract DiscoveredTest BuildDiscoveredTest(TestContext testContext);

    public abstract TestMetadata CloneWithNewMethodFactory(Func<object, CancellationToken, Task> testMethodFactory);
}