using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Base class for test definitions.
/// </summary>
public abstract record TestDefinitionBase : ITestDefinition
{
    /// <summary>
    /// Internal method to build discovered tests.
    /// </summary>
    internal abstract IEnumerable<DiscoveredTest> BuildTests(ITestBuilder builder);

    /// <summary>
    /// Unique identifier for the test.
    /// </summary>
    public abstract string TestId { get; init; }

    /// <summary>
    /// Metadata about the test method.
    /// </summary>
    public abstract MethodMetadata MethodMetadata { get; init; }

    /// <summary>
    /// Source file path where the test is defined.
    /// </summary>
    public abstract string TestFilePath { get; init; }

    /// <summary>
    /// Line number in the source file where the test is defined.
    /// </summary>
    public abstract int TestLineNumber { get; init; }

    /// <summary>
    /// Data provider for class-level test data.
    /// </summary>
    public abstract IDataProvider ClassDataProvider { get; init; }

    /// <summary>
    /// Data provider for method-level test data.
    /// </summary>
    public abstract IDataProvider MethodDataProvider { get; init; }
}

/// <summary>
/// Immutable definition of a test - represents what a test is, not how it runs.
/// </summary>
public sealed record TestDefinition : TestDefinitionBase
{
    /// <summary>
    /// Unique identifier for the test.
    /// </summary>
    public override required string TestId { get; init; }

    /// <summary>
    /// Metadata about the test method.
    /// </summary>
    public override required MethodMetadata MethodMetadata { get; init; }

    /// <summary>
    /// Source file path where the test is defined.
    /// </summary>
    public override required string TestFilePath { get; init; }

    /// <summary>
    /// Line number in the source file where the test is defined.
    /// </summary>
    public override required int TestLineNumber { get; init; }

    /// <summary>
    /// Factory to create instances of the test class.
    /// </summary>
    public required Func<object> TestClassFactory { get; init; }

    /// <summary>
    /// Factory to invoke the test method on a class instance.
    /// </summary>
    public required Func<object, CancellationToken, ValueTask> TestMethodInvoker { get; init; }

    /// <summary>
    /// Provider for properties to inject into the test class.
    /// </summary>
    public required Func<IDictionary<string, object?>> PropertiesProvider { get; init; }

    /// <summary>
    /// Data provider for class-level test data.
    /// </summary>
    public override required IDataProvider ClassDataProvider { get; init; }

    /// <summary>
    /// Data provider for method-level test data.
    /// </summary>
    public override required IDataProvider MethodDataProvider { get; init; }
    
    /// <summary>
    /// Original factory to create test class instances with arguments.
    /// </summary>
    public Func<object?[], object>? OriginalClassFactory { get; init; }
    
    /// <summary>
    /// Original invoker for the test method with arguments.
    /// </summary>
    public Func<object, object?[], CancellationToken, Task>? OriginalMethodInvoker { get; init; }

    /// <summary>
    /// Builds discovered tests using the provided builder.
    /// </summary>
    internal override IEnumerable<DiscoveredTest> BuildTests(ITestBuilder builder)
    {
        return builder.BuildTests(this);
    }
}

/// <summary>
/// Generic version of TestDefinition that maintains type safety for AOT/trimming.
/// </summary>
public sealed record TestDefinition<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTestClass>
    : TestDefinitionBase
    where TTestClass : class
{
    /// <summary>
    /// Unique identifier for the test.
    /// </summary>
    public override required string TestId { get; init; }

    /// <summary>
    /// Metadata about the test method.
    /// </summary>
    public override required MethodMetadata MethodMetadata { get; init; }

    /// <summary>
    /// Source file path where the test is defined.
    /// </summary>
    public override required string TestFilePath { get; init; }

    /// <summary>
    /// Line number in the source file where the test is defined.
    /// </summary>
    public override required int TestLineNumber { get; init; }

    /// <summary>
    /// Strongly-typed factory to create instances of the test class.
    /// </summary>
    public required Func<TTestClass> TestClassFactory { get; init; }

    /// <summary>
    /// Strongly-typed factory to invoke the test method on a class instance.
    /// </summary>
    public required Func<TTestClass, CancellationToken, ValueTask> TestMethodInvoker { get; init; }

    /// <summary>
    /// Provider for properties to inject into the test class.
    /// </summary>
    public required Func<IDictionary<string, object?>> PropertiesProvider { get; init; }

    /// <summary>
    /// Data provider for class-level test data.
    /// </summary>
    public override required IDataProvider ClassDataProvider { get; init; }

    /// <summary>
    /// Data provider for method-level test data.
    /// </summary>
    public override required IDataProvider MethodDataProvider { get; init; }
    
    /// <summary>
    /// Original factory to create test class instances with arguments.
    /// </summary>
    public Func<object?[], TTestClass>? OriginalClassFactory { get; init; }
    
    /// <summary>
    /// Original invoker for the test method with arguments.
    /// </summary>
    public Func<TTestClass, object?[], CancellationToken, Task>? OriginalMethodInvoker { get; init; }

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
            MethodMetadata = definition.MethodMetadata,
            TestFilePath = definition.TestFilePath,
            TestLineNumber = definition.TestLineNumber,
            TestClassFactory = () => definition.TestClassFactory(),
            TestMethodInvoker = (obj, ct) => definition.TestMethodInvoker((TTestClass)obj, ct),
            PropertiesProvider = definition.PropertiesProvider,
            ClassDataProvider = definition.ClassDataProvider,
            MethodDataProvider = definition.MethodDataProvider,
            OriginalClassFactory = definition.OriginalClassFactory != null 
                ? args => definition.OriginalClassFactory(args) 
                : null,
            OriginalMethodInvoker = definition.OriginalMethodInvoker != null 
                ? (obj, args, ct) => definition.OriginalMethodInvoker((TTestClass)obj, args, ct)
                : null
        };
    }

    /// <summary>
    /// Builds discovered tests using the provided builder.
    /// This method dispatches to the generic version on the builder, avoiding reflection.
    /// </summary>
    internal override IEnumerable<DiscoveredTest> BuildTests(ITestBuilder builder)
    {
        // This is the key: we know our generic type at compile time here,
        // so we can call the generic method directly without reflection
        return builder.BuildTests(this);
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
    MethodMetadata MethodMetadata { get; }

    /// <summary>
    /// Source file path where the test is defined.
    /// </summary>
    string TestFilePath { get; }

    /// <summary>
    /// Line number in the source file where the test is defined.
    /// </summary>
    int TestLineNumber { get; }

    /// <summary>
    /// Data provider for class-level test data.
    /// </summary>
    IDataProvider ClassDataProvider { get; }

    /// <summary>
    /// Data provider for method-level test data.
    /// </summary>
    IDataProvider MethodDataProvider { get; }
}

/// <summary>
/// Interface for test builders that can construct discovered tests.
/// This interface is internal to avoid exposing DiscoveredTest publicly.
/// </summary>
internal interface ITestBuilder
{
    /// <summary>
    /// Builds discovered tests from a non-generic test definition.
    /// </summary>
    IEnumerable<DiscoveredTest> BuildTests(TestDefinition definition);

    /// <summary>
    /// Builds discovered tests from a generic test definition.
    /// </summary>
    IEnumerable<DiscoveredTest<TTestClass>> BuildTests<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTestClass>(
        TestDefinition<TTestClass> definition) where TTestClass : class;
}

