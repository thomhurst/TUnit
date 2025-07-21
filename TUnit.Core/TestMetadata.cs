using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Unified metadata for a test, fully AOT-compatible with no reflection dependencies
/// </summary>
public abstract class TestMetadata
{
    public required string TestName { get; init; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public required Type TestClassType { get; init; }

    public required string TestMethodName { get; init; }

    public string[] Categories { get; init; } = [];

    public bool IsSkipped { get; init; }

    public string? SkipReason { get; init; }

    /// <summary>
    /// Test timeout in milliseconds (null for no timeout)
    /// </summary>
    public int? TimeoutMs { get; init; }

    public int RetryCount { get; init; }

    public bool CanRunInParallel { get; init; } = true;

    /// <summary>
    /// Test dependencies with full metadata support for generic types and methods
    /// </summary>
    public TestDependency[] Dependencies { get; init; } = [];

    /// <summary>
    /// Test data for parameterized tests
    /// </summary>
    public IDataSourceAttribute[] DataSources { get; init; } = [];

    /// <summary>
    /// Class-level data sources for constructor arguments
    /// </summary>
    public IDataSourceAttribute[] ClassDataSources { get; init; } = [];

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

    public int ParameterCount { get; init; }

    public Type[] ParameterTypes { get; init; } = [];

    /// <summary>
    /// Parameter type names for dependency matching (fully qualified type names)
    /// </summary>
    public string[] TestMethodParameterTypes { get; init; } = [];

    /// <summary>
    /// Hooks to run at various test lifecycle points
    /// </summary>
    public TestHooks Hooks { get; init; } = new();

    public string? FilePath { get; init; }

    public int? LineNumber { get; init; }

    public required MethodMetadata MethodMetadata { get; init; }

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
    /// Both AOT and reflection modes must provide delegates with identical signatures.
    /// The delegates encapsulate all mode-specific behavior.
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
    public required string ParameterName { get; init; }

    public Type? BaseTypeConstraint { get; init; }

    public Type[] InterfaceConstraints { get; init; } = [];

    public bool HasDefaultConstructorConstraint { get; init; }

    public bool HasReferenceTypeConstraint { get; init; }

    public bool HasValueTypeConstraint { get; init; }

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
    
    /// <summary>
    /// Nested property injection data for recursive property injection.
    /// Empty array if no nested properties need injection.
    /// </summary>
    public PropertyInjectionData[] NestedPropertyInjections { get; init; } = Array.Empty<PropertyInjectionData>();
    
    /// <summary>
    /// Factory to extract nested property values from the parent object.
    /// Returns a dictionary mapping property names to their values for nested injection.
    /// </summary>
    public Func<object?, Dictionary<string, object?>>? NestedPropertyValueFactory { get; init; }
}
