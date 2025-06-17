using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Immutable definition of a test - represents what a test is, not how it runs.
/// </summary>
public sealed record TestDefinition : ITestDefinition
{
    /// <summary>
    /// Unique identifier for the test.
    /// </summary>
    public required string TestId { get; init; }
    
    /// <summary>
    /// Metadata about the test method.
    /// </summary>
    public required TestMethod TestMethod { get; init; }
    
    /// <summary>
    /// How many times this test should be repeated.
    /// </summary>
    public required int RepeatCount { get; init; }
    
    /// <summary>
    /// Source file path where the test is defined.
    /// </summary>
    public required string TestFilePath { get; init; }
    
    /// <summary>
    /// Line number in the source file where the test is defined.
    /// </summary>
    public required int TestLineNumber { get; init; }
    
    /// <summary>
    /// Factory to create instances of the test class.
    /// </summary>
    public required Func<object> TestClassFactory { get; init; }
    
    /// <summary>
    /// Factory to invoke the test method on a class instance.
    /// </summary>
    public required Func<object, CancellationToken, ValueTask> TestMethodInvoker { get; init; }
    
    /// <summary>
    /// Provider for test class constructor arguments.
    /// </summary>
    public required Func<object?[]> ClassArgumentsProvider { get; init; }
    
    /// <summary>
    /// Provider for test method arguments.
    /// </summary>
    public required Func<object?[]> MethodArgumentsProvider { get; init; }
    
    /// <summary>
    /// Provider for properties to inject into the test class.
    /// </summary>
    public required Func<IDictionary<string, object?>> PropertiesProvider { get; init; }
    
    /// <summary>
    /// Builds a discovered test using the provided builder.
    /// </summary>
    public DiscoveredTest BuildTest(ITestBuilder builder, int currentRepeatAttempt)
    {
        return builder.BuildTest(this, currentRepeatAttempt);
    }
}

/// <summary>
/// Generic version of TestDefinition that maintains type safety for AOT/trimming.
/// </summary>
public sealed record TestDefinition<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTestClass>
    : ITestDefinition
    where TTestClass : class
{
    /// <summary>
    /// Unique identifier for the test.
    /// </summary>
    public required string TestId { get; init; }
    
    /// <summary>
    /// Metadata about the test method.
    /// </summary>
    public required TestMethod TestMethod { get; init; }
    
    /// <summary>
    /// How many times this test should be repeated.
    /// </summary>
    public required int RepeatCount { get; init; }
    
    /// <summary>
    /// Source file path where the test is defined.
    /// </summary>
    public required string TestFilePath { get; init; }
    
    /// <summary>
    /// Line number in the source file where the test is defined.
    /// </summary>
    public required int TestLineNumber { get; init; }
    
    /// <summary>
    /// Strongly-typed factory to create instances of the test class.
    /// </summary>
    public required Func<TTestClass> TestClassFactory { get; init; }
    
    /// <summary>
    /// Strongly-typed factory to invoke the test method on a class instance.
    /// </summary>
    public required Func<TTestClass, CancellationToken, ValueTask> TestMethodInvoker { get; init; }
    
    /// <summary>
    /// Provider for test class constructor arguments.
    /// </summary>
    public required Func<object?[]> ClassArgumentsProvider { get; init; }
    
    /// <summary>
    /// Provider for test method arguments.
    /// </summary>
    public required Func<object?[]> MethodArgumentsProvider { get; init; }
    
    /// <summary>
    /// Provider for properties to inject into the test class.
    /// </summary>
    public required Func<IDictionary<string, object?>> PropertiesProvider { get; init; }
    
    /// <summary>
    /// Gets the type of the test class for AOT.
    /// </summary>
    public Type TestClassType => typeof(TTestClass);
    
    /// <summary>
    /// Converts this generic TestDefinition to the base non-generic version.
    /// </summary>
    public static implicit operator TestDefinition(TestDefinition<TTestClass> definition)
    {
        return new TestDefinition
        {
            TestId = definition.TestId,
            TestMethod = definition.TestMethod,
            RepeatCount = definition.RepeatCount,
            TestFilePath = definition.TestFilePath,
            TestLineNumber = definition.TestLineNumber,
            TestClassFactory = () => definition.TestClassFactory(),
            TestMethodInvoker = (obj, ct) => definition.TestMethodInvoker((TTestClass)obj, ct),
            ClassArgumentsProvider = definition.ClassArgumentsProvider,
            MethodArgumentsProvider = definition.MethodArgumentsProvider,
            PropertiesProvider = definition.PropertiesProvider
        };
    }
    
    /// <summary>
    /// Builds a discovered test using the provided builder.
    /// This method dispatches to the generic version on the builder, avoiding reflection.
    /// </summary>
    public DiscoveredTest BuildTest(ITestBuilder builder, int currentRepeatAttempt)
    {
        // This is the key: we know our generic type at compile time here,
        // so we can call the generic method directly without reflection
        return builder.BuildTest(this, currentRepeatAttempt);
    }
}

/// <summary>
/// Common interface for test definitions.
/// </summary>
public interface ITestDefinition
{
    /// <summary>
    /// Unique identifier for the test.
    /// </summary>
    string TestId { get; }
    
    /// <summary>
    /// Metadata about the test method.
    /// </summary>
    TestMethod TestMethod { get; }
    
    /// <summary>
    /// How many times this test should be repeated.
    /// </summary>
    int RepeatCount { get; }
    
    /// <summary>
    /// Source file path where the test is defined.
    /// </summary>
    string TestFilePath { get; }
    
    /// <summary>
    /// Line number in the source file where the test is defined.
    /// </summary>
    int TestLineNumber { get; }
    
    /// <summary>
    /// Builds a discovered test using the provided builder.
    /// This allows TestDefinition<T> to dispatch to the correct generic method.
    /// </summary>
    DiscoveredTest BuildTest(ITestBuilder builder, int currentRepeatAttempt);
}

/// <summary>
/// Interface for test builders that can construct discovered tests.
/// </summary>
public interface ITestBuilder
{
    /// <summary>
    /// Builds a discovered test from a non-generic test definition.
    /// </summary>
    DiscoveredTest BuildTest(TestDefinition definition, int currentRepeatAttempt);
    
    /// <summary>
    /// Builds a discovered test from a generic test definition.
    /// </summary>
    DiscoveredTest<TTestClass> BuildTest<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTestClass>(
        TestDefinition<TTestClass> definition, 
        int currentRepeatAttempt) where TTestClass : class;
}