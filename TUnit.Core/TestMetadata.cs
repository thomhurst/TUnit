using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Represents the metadata for a test.
/// </summary>
/// <typeparam name="TClassType">The type of the test class.</typeparam>
public record TestMetadata<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClassType> : TestMetadata where TClassType : class
{
    /// <summary>
    /// Gets or sets the resettable class factory.
    /// </summary>
    public required ResettableLazy<TClassType> ResettableClassFactory { get; init; }

    /// <summary>
    /// Gets or sets the test method factory.
    /// </summary>
    public required Func<TClassType, CancellationToken, ValueTask> TestMethodFactory { get; init; }

    /// <inheritdoc />
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
			DataAttributes = TestBuilderContext.DataAttributes.OfType<Attribute>().ToArray()
		};

		return testDetails;
    }

    /// <inheritdoc />
    internal override DiscoveredTest BuildDiscoveredTest(TestContext testContext)
    {
	    return new DiscoveredTest<TClassType>(ResettableClassFactory)
	    {
		    TestContext = testContext,
		    TestBody = (classInstance, cancellationToken) => TestMethodFactory(classInstance, cancellationToken),
	    };
    }

    /// <inheritdoc />
    public override TestMetadata CloneWithNewMethodFactory(Func<object, CancellationToken, ValueTask> testMethodFactory)
    {
	    return this with
	    {
		    TestMethodFactory = testMethodFactory.Invoke,
		    ResettableClassFactory = ResettableClassFactory.Clone()
	    };
    }
}

/// <summary>
/// Represents the base metadata for a test.
/// </summary>
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
    
    public Exception? DiscoveryException { get; init; }
    
    public abstract TestDetails BuildTestDetails();
    internal abstract DiscoveredTest BuildDiscoveredTest(TestContext testContext);

    public abstract TestMetadata CloneWithNewMethodFactory(Func<object, CancellationToken, ValueTask> testMethodFactory);
}