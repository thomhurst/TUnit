using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

/// <summary>
/// Represents a statically-defined test that is fully AOT and trimming compatible.
/// All type information and delegates are resolved at compile time by the source generator.
/// </summary>
public sealed class StaticTestDefinition : ITestDescriptor
{
    // ITestDescriptor implementation
    public required string TestId { get; init; }
    public required string DisplayName { get; init; }
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }
    public required bool IsAsync { get; init; }
    public required bool IsSkipped { get; init; }
    public string? SkipReason { get; init; }
    public TimeSpan? Timeout { get; init; }
    public required int RepeatCount { get; init; }
    
    /// <summary>
    /// The concrete test class type. Always known at compile time for static tests.
    /// </summary>
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicConstructors |
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.PublicProperties)]
    public required Type TestClassType { get; init; }
    
    /// <summary>
    /// The test method metadata. Always known at compile time for static tests.
    /// </summary>
    public required MethodMetadata TestMethodMetadata { get; init; }
    
    /// <summary>
    /// Factory to create test class instances. Generated at compile time.
    /// Takes constructor arguments array and returns typed instance.
    /// </summary>
    public required Func<object?[], object> ClassFactory { get; init; }
    
    /// <summary>
    /// Invoker for the test method. Generated at compile time.
    /// Takes test instance, method arguments array, and cancellation token, returns Task.
    /// </summary>
    public required Func<object, object?[], CancellationToken, Task> MethodInvoker { get; init; }
    
    /// <summary>
    /// Provides property values for each test iteration. Key is property name.
    /// </summary>
    public required Func<IEnumerable<IDictionary<string, object?>>> PropertyValuesProvider { get; init; }
    
    /// <summary>
    /// Data provider for class-level test data.
    /// </summary>
    public required IDataProvider ClassDataProvider { get; init; }
    
    /// <summary>
    /// Data provider for method-level test data.
    /// </summary>
    public required IDataProvider MethodDataProvider { get; init; }
}