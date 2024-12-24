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
		var methodInfo = MethodInfo;
		var classType = typeof(TClassType);

		var dataAttributes = DataAttributes;
		Attribute[] attributes = [..TestAttributes, ..ClassAttributes, ..AssemblyAttributes];
		
		var testDetails = new TestDetails<TClassType>
		{
			TestId = testId,
			LazyClassInstance = ResettableClassFactory,
			ClassType = classType,
			AssemblyAttributes = AssemblyAttributes,
			ClassAttributes = ClassAttributes,
			TestAttributes = TestAttributes,
			DataAttributes = dataAttributes,
			Attributes = attributes,
			TestClassArguments = TestClassArguments,
			TestMethodArguments = TestMethodArguments,
			TestClassInjectedPropertyArguments = TestClassProperties,
			TestClassParameterTypes = classType.GetConstructors().FirstOrDefault()?.GetParameters().Select(x => x.ParameterType).ToArray() ?? [],
			TestMethodParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray(),
			CurrentRepeatAttempt = CurrentRepeatAttempt,
			RepeatLimit = RepeatLimit,
			MethodInfo = methodInfo,
			TestName = methodInfo.Name,
			ReturnType = methodInfo.ReturnType,
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
    public required MethodInfo MethodInfo { get; init; }
    
    public required int RepeatLimit { get; init; }
    public required int CurrentRepeatAttempt { get; init; }
    
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }
    
    public required object?[] TestClassArguments { get; init; }
    public required object?[] TestMethodArguments { get; init; }
    public required object?[] TestClassProperties { get; init; }
    
    // Need to be referenced statically for AOT
    public required Attribute[] TestAttributes { get; init; }
    public required Attribute[] ClassAttributes { get; init; }
    public required Attribute[] AssemblyAttributes { get; init; }
    
    public required Attribute[] DataAttributes { get; init; }
    
    public required TestBuilderContext TestBuilderContext { get; init; }
    
    public abstract TestDetails BuildTestDetails();
    internal abstract DiscoveredTest BuildDiscoveredTest(TestContext testContext);

    public abstract TestMetadata CloneWithNewMethodFactory(Func<object, CancellationToken, Task> testMethodFactory);
}