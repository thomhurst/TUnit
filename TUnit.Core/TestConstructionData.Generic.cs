using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Generic version of TestConstructionData that maintains type safety for AOT/trimming.
/// </summary>
public record TestConstructionData<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTestClass>
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
    /// Total number of times to repeat the test.
    /// </summary>
    public required int RepeatCount { get; init; }
    
    /// <summary>
    /// Current repeat attempt number (1-based).
    /// </summary>
    public required int CurrentRepeatAttempt { get; init; }
    
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
    /// Test builder context containing additional metadata.
    /// </summary>
    public required TestBuilderContext TestBuilderContext { get; init; }
    
    /// <summary>
    /// Exception that occurred during test discovery, if any.
    /// </summary>
    public Exception? DiscoveryException { get; init; }
}