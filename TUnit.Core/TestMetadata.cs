namespace TUnit.Core;

/// <summary>
/// Unified metadata for a test, fully AOT-compatible with no reflection dependencies
/// </summary>
public abstract class TestMetadata
{
    /// <summary>
    /// Unique identifier for the test
    /// </summary>
    public required string TestId { get; init; }

    /// <summary>
    /// Display name for the test
    /// </summary>
    public required string TestName { get; init; }

    /// <summary>
    /// The type containing the test method
    /// </summary>
    public required Type TestClassType { get; init; }

    /// <summary>
    /// The test method name
    /// </summary>
    public required string TestMethodName { get; init; }

    /// <summary>
    /// Test categories for filtering
    /// </summary>
    public string[] Categories { get; init; } = [];

    /// <summary>
    /// Whether this test should be skipped
    /// </summary>
    public bool IsSkipped { get; init; }

    /// <summary>
    /// Skip reason if IsSkipped is true
    /// </summary>
    public string? SkipReason { get; init; }

    /// <summary>
    /// Test timeout in milliseconds (null for no timeout)
    /// </summary>
    public int? TimeoutMs { get; init; }

    /// <summary>
    /// Number of retry attempts allowed
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// Whether this test can run in parallel
    /// </summary>
    public bool CanRunInParallel { get; init; } = true;

    /// <summary>
    /// Test dependencies with full metadata support for generic types and methods
    /// </summary>
    public TestDependency[] Dependencies { get; init; } = [];

    /// <summary>
    /// Test data for parameterized tests
    /// </summary>
    public TestDataSource[] DataSources { get; init; } = [];

    /// <summary>
    /// Class-level data sources for constructor arguments
    /// </summary>
    public TestDataSource[] ClassDataSources { get; init; } = [];

    /// <summary>
    /// Properties that require data injection
    /// </summary>
    public PropertyDataSource[] PropertyDataSources { get; init; } = [];

    /// <summary>
    /// AOT-safe factory to create test class instance
    /// Accepts constructor arguments array (empty array for parameterless constructors)
    /// </summary>
    public Func<object?[], object>? InstanceFactory { get; init; }

    /// <summary>
    /// AOT-safe test method invoker
    /// Returns Task for all test methods (sync methods wrapped in Task.CompletedTask)
    /// </summary>
    public Func<object, object?[], Task>? TestInvoker { get; init; }

    /// <summary>
    /// Number of parameters the test method expects
    /// </summary>
    public int ParameterCount { get; init; }

    /// <summary>
    /// Parameter types for validation
    /// </summary>
    public Type[] ParameterTypes { get; init; } = [];

    /// <summary>
    /// Parameter type names for dependency matching (fully qualified type names)
    /// </summary>
    public string[] TestMethodParameterTypes { get; init; } = [];

    /// <summary>
    /// Hooks to run at various test lifecycle points
    /// </summary>
    public TestHooks Hooks { get; init; } = new();

    /// <summary>
    /// Source file path where test is defined
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Line number where test is defined
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// Metadata about the test method
    /// </summary>
    public MethodMetadata? MethodMetadata { get; init; }

    /// <summary>
    /// Generic type information if the test class is generic
    /// </summary>
    public GenericTypeInfo? GenericTypeInfo { get; init; }

    /// <summary>
    /// Generic method information if the test method is generic
    /// </summary>
    public GenericMethodInfo? GenericMethodInfo { get; init; }

    /// <summary>
    /// Concrete type arguments for generic method instantiation (used with [GenerateGenericTest])
    /// </summary>
    public Type[]? GenericMethodTypeArguments { get; init; }

    /// <summary>
    /// Factory to create attribute instances applied to this test (for discovery event receivers)
    /// </summary>
    public required Func<Attribute[]> AttributeFactory { get; init; }

    /// <summary>
    /// Property setters dictionary for property injection
    /// Key is property name, value is setter delegate
    /// </summary>
    public Dictionary<string, Action<object, object?>> PropertySetters { get; init; } = new();

    /// <summary>
    /// Enhanced property injection data including setters and value factories
    /// </summary>
    public PropertyInjectionData[] PropertyInjections { get; init; } = [];

    /// <summary>
    /// Generator delegate that produces all data combinations for this test.
    /// Used by TestBuilder to expand test data without reflection.
    /// </summary>
    public abstract Func<IAsyncEnumerable<TestDataCombination>> DataCombinationGenerator { get; }

    /// <summary>
    /// Factory delegate that creates an ExecutableTest for this metadata.
    /// Used by TestBuilder to create strongly-typed executable tests without reflection.
    /// Must never be null.
    /// </summary>
    public abstract Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest> CreateExecutableTestFactory { get; }
}

// TestDataSource classes have been moved to TestDataSources.cs
// Import the classes from the new location

/// <summary>
/// Test lifecycle hooks
/// </summary>
public sealed class TestHooks
{
    /// <summary>
    /// Hooks to run before the test class is instantiated
    /// </summary>
    public HookMetadata[] BeforeClass { get; init; } = [];

    /// <summary>
    /// Hooks to run after the test class is instantiated
    /// </summary>
    public HookMetadata[] AfterClass { get; init; } = [];

    /// <summary>
    /// Hooks to run before each test
    /// </summary>
    public HookMetadata[] BeforeTest { get; init; } = [];

    /// <summary>
    /// Hooks to run after each test
    /// </summary>
    public HookMetadata[] AfterTest { get; init; } = [];
}

/// <summary>
/// Metadata for a lifecycle hook
/// </summary>
public sealed class HookMetadata
{
    public required string Name { get; init; }
    public required HookLevel Level { get; init; }
    public int Order { get; init; }

    /// <summary>
    /// Hook delegate key for AOT-safe invocation
    /// </summary>
    public string? DelegateKey { get; init; }

    /// <summary>
    /// Type that declares this hook
    /// </summary>
    public Type? DeclaringType { get; init; }

    /// <summary>
    /// Whether this is a static hook
    /// </summary>
    public bool IsStatic { get; init; }

    /// <summary>
    /// Whether this hook is async (returns Task or ValueTask)
    /// </summary>
    public bool IsAsync { get; init; }

    /// <summary>
    /// Whether this hook returns ValueTask (requires special handling)
    /// </summary>
    public bool ReturnsValueTask { get; init; }

    /// <summary>
    /// Hook delegate from storage (AOT mode)
    /// </summary>
    public Func<object, TestContext, Task>? HookInvoker { get; init; }
}

public enum HookLevel
{
    Assembly,
    Class,
    Test
}

/// <summary>
/// Information about generic type parameters on a test class
/// </summary>
public sealed class GenericTypeInfo
{
    /// <summary>
    /// Names of the generic type parameters (e.g., ["T", "U"])
    /// </summary>
    public string[] ParameterNames { get; init; } = [];

    /// <summary>
    /// Constraints for each generic parameter
    /// </summary>
    public GenericParameterConstraints[] Constraints { get; init; } = [];
}

/// <summary>
/// Information about generic type parameters on a test method
/// </summary>
public sealed class GenericMethodInfo
{
    /// <summary>
    /// Names of the generic type parameters (e.g., ["T", "U"])
    /// </summary>
    public string[] ParameterNames { get; init; } = [];

    /// <summary>
    /// Constraints for each generic parameter
    /// </summary>
    public GenericParameterConstraints[] Constraints { get; init; } = [];

    /// <summary>
    /// Maps generic parameters to method argument positions for type inference
    /// </summary>
    public int[] ParameterPositions { get; init; } = [];
}

/// <summary>
/// Constraints for a generic type parameter
/// </summary>
public sealed class GenericParameterConstraints
{
    /// <summary>
    /// The generic parameter name
    /// </summary>
    public required string ParameterName { get; init; }

    /// <summary>
    /// Base type constraint (if any)
    /// </summary>
    public Type? BaseTypeConstraint { get; init; }

    /// <summary>
    /// Interface constraints
    /// </summary>
    public Type[] InterfaceConstraints { get; init; } = [];

    /// <summary>
    /// Whether the parameter has a new() constraint
    /// </summary>
    public bool HasDefaultConstructorConstraint { get; init; }

    /// <summary>
    /// Whether the parameter has a class constraint
    /// </summary>
    public bool HasReferenceTypeConstraint { get; init; }

    /// <summary>
    /// Whether the parameter has a struct constraint
    /// </summary>
    public bool HasValueTypeConstraint { get; init; }

    /// <summary>
    /// Whether the parameter has a notnull constraint
    /// </summary>
    public bool HasNotNullConstraint { get; init; }
}

/// <summary>
/// Data for property injection including setter and value factory
/// </summary>
public sealed class PropertyInjectionData
{
    public required string PropertyName { get; init; }
    public required Type PropertyType { get; init; }
    public required Action<object, object?> Setter { get; init; }
    public required Func<object?> ValueFactory { get; init; }
}
