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

    public TestDependency[] Dependencies { get; init; } = [];

    public required IDataSourceAttribute[] DataSources { get; init; } = [];

    public required IDataSourceAttribute[] ClassDataSources { get; init; } = [];

    public required PropertyDataSource[] PropertyDataSources { get; init; } = [];

    /// <summary>
    /// AOT-safe factory to create test class instance
    /// Accepts type arguments for generic types and constructor arguments array
    /// For non-generic types, typeArgs will be Type.EmptyTypes
    /// </summary>
    public Func<Type[], object?[], object> InstanceFactory { get; init; } = null!;

    /// <summary>
    /// AOT-safe test method invoker
    /// Returns Task for all test methods (sync methods wrapped in Task.CompletedTask)
    /// </summary>
    public Func<object, object?[], Task>? TestInvoker { get; init; }

    public string? FilePath { get; init; }

    public int? LineNumber { get; init; }

    public required MethodMetadata MethodMetadata { get; init; }

    public GenericTypeInfo? GenericTypeInfo { get; init; }

    public GenericMethodInfo? GenericMethodInfo { get; init; }

    public Type[]? GenericMethodTypeArguments { get; init; }

    public required Func<Attribute[]> AttributeFactory { get; init; }

    public PropertyInjectionData[] PropertyInjections { get; init; } = [];

    /// <summary>
    /// Test session ID used for data generation
    /// </summary>
    public string TestSessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Factory delegate that creates an ExecutableTest for this metadata.
    /// Both AOT and reflection modes must provide delegates with identical signatures.
    /// The delegates encapsulate all mode-specific behavior.
    /// </summary>
    public abstract Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory { get; }
}

public sealed class GenericTypeInfo
{
    public string[] ParameterNames { get; init; } = [];

    public GenericParameterConstraints[] Constraints { get; init; } = [];
}

public sealed class GenericMethodInfo
{
    public string[] ParameterNames { get; init; } = [];

    public GenericParameterConstraints[] Constraints { get; init; } = [];

    public int[] ParameterPositions { get; init; } = [];
}

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

public sealed class PropertyInjectionData
{
    public required string PropertyName { get; init; }
    public required Type PropertyType { get; init; }
    public required Action<object, object?> Setter { get; init; }
    public required Func<object?> ValueFactory { get; init; }
    public PropertyInjectionData[] NestedPropertyInjections { get; init; } = [
    ];

    /// <summary>
    /// Factory to extract nested property values from the parent object.
    /// Returns a dictionary mapping property names to their values for nested injection.
    /// </summary>
    public Func<object?, Dictionary<string, object?>>? NestedPropertyValueFactory { get; init; }
}
