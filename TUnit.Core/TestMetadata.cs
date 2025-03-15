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
    /// <summary>
    /// Gets or sets the test ID.
    /// </summary>
    public required string TestId { get; init; }
    
    /// <summary>
    /// Gets or sets the test method information.
    /// </summary>
    public required SourceGeneratedMethodInformation TestMethod { get; init; }
    
    /// <summary>
    /// Gets or sets the repeat limit for the test.
    /// </summary>
    public required int RepeatLimit { get; init; }
    
    /// <summary>
    /// Gets or sets the current repeat attempt for the test.
    /// </summary>
    public required int CurrentRepeatAttempt { get; init; }
    
    /// <summary>
    /// Gets or sets the file path of the test.
    /// </summary>
    public required string TestFilePath { get; init; }
    
    /// <summary>
    /// Gets or sets the line number of the test.
    /// </summary>
    public required int TestLineNumber { get; init; }
    
    /// <summary>
    /// Gets or sets the arguments for the test class.
    /// </summary>
    public required object?[] TestClassArguments { get; init; }
    
    /// <summary>
    /// Gets or sets the arguments for the test method.
    /// </summary>
    public required object?[] TestMethodArguments { get; init; }
    
    /// <summary>
    /// Gets or sets the properties for the test class.
    /// </summary>
    public required object?[] TestClassProperties { get; init; }
    
    /// <summary>
    /// Gets or sets the test builder context.
    /// </summary>
    public required TestBuilderContext TestBuilderContext { get; init; }
    
    /// <summary>
    /// Gets or sets the discovery exception, if any.
    /// </summary>
    public Exception? DiscoveryException { get; init; }
    
    /// <summary>
    /// Builds the test details.
    /// </summary>
    /// <returns>The test details.</returns>
    public abstract TestDetails BuildTestDetails();
    
    /// <summary>
    /// Builds the discovered test.
    /// </summary>
    /// <param name="testContext">The test context.</param>
    /// <returns>The discovered test.</returns>
    internal abstract DiscoveredTest BuildDiscoveredTest(TestContext testContext);

    /// <summary>
    /// Clones the test metadata with a new method factory.
    /// </summary>
    /// <param name="testMethodFactory">The new test method factory.</param>
    /// <returns>The cloned test metadata.</returns>
    public abstract TestMetadata CloneWithNewMethodFactory(Func<object, CancellationToken, ValueTask> testMethodFactory);
}