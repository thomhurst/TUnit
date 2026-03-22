using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// A self-contained test registration unit emitted by the source generator.
/// Contains everything needed to filter, materialize, and execute a single test:
/// pure data for filtering, behavioral delegates for execution, and property
/// descriptors for injection — all without reflection at runtime.
/// </summary>
/// <remarks>
/// <para>
/// Delegate properties (InvokeBody, CreateAttributes, CreateInstance) are shared
/// across all entries in a class — they point to class-level switch methods.
/// Per-test differentiation is via MethodIndex and AttributeGroupIndex.
/// This means zero per-test methods in the generated assembly.
/// </para>
/// </remarks>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public sealed class TestEntry<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.PublicMethods)] T> where T : class
{
    // --- Identity (pure data for filtering — no JIT needed) ---

    /// <summary>Test method name.</summary>
    public required string MethodName { get; init; }

    /// <summary>Fully qualified name: Namespace.Class.Method</summary>
    public required string FullyQualifiedName { get; init; }

    /// <summary>Source file path.</summary>
    public required string FilePath { get; init; }

    /// <summary>Source line number.</summary>
    public required int LineNumber { get; init; }

    /// <summary>Pre-extracted categories for fast filtering.</summary>
    public string[] Categories { get; init; } = [];

    /// <summary>Pre-extracted "key=value" property pairs for fast filtering.</summary>
    public string[] CustomProperties { get; init; } = [];

    /// <summary>Dependency strings for fast BFS filtering: "ClassName:MethodName".</summary>
    public string[] DependsOn { get; init; } = [];

    /// <summary>Pre-built TestDependency objects for the engine's dependency resolver.</summary>
    public TestDependency[] Dependencies { get; init; } = [];

    /// <summary>Whether this test has data sources.</summary>
    public bool HasDataSource { get; init; }

    /// <summary>Repeat count from RepeatAttribute, or 0.</summary>
    public int RepeatCount { get; init; }

    // --- Structural metadata (pre-built by source generator in .cctor) ---

    /// <summary>Pre-built method metadata (name, return type, parameters, class metadata).</summary>
    public required MethodMetadata MethodMetadata { get; init; }

    // --- Class-level shared delegates (1 JIT each, shared by ALL entries in this class) ---

    /// <summary>
    /// Consolidated class-level invoker. All tests in the class share this delegate;
    /// each test dispatches via its MethodIndex. One JIT per class.
    /// </summary>
    public required Func<T, int, object?[], CancellationToken, ValueTask> InvokeBody { get; init; }

    /// <summary>Method index for InvokeBody dispatch.</summary>
    public required int MethodIndex { get; init; }

    /// <summary>
    /// Consolidated class-level attribute factory. All tests in the class share this delegate;
    /// each test dispatches via its AttributeGroupIndex. One JIT per class.
    /// </summary>
    public required Func<int, Attribute[]> CreateAttributes { get; init; }

    /// <summary>Attribute group index for CreateAttributes dispatch.</summary>
    public required int AttributeGroupIndex { get; init; }

    /// <summary>AOT-safe factory to create test class instances. Shared by all entries in class.</summary>
    public required Func<Type[], object?[], T> CreateInstance { get; init; }

    // --- Data sources (pre-separated by source generator — no runtime scanning needed) ---

    /// <summary>Method-level data source attributes (Arguments, MethodDataSource, etc.).</summary>
    public IDataSourceAttribute[] TestDataSources { get; init; } = [];

    /// <summary>Class-level data source attributes.</summary>
    public IDataSourceAttribute[] ClassDataSources { get; init; } = [];

    // --- Property injection (source-generated, no reflection needed) ---

    /// <summary>Properties with data source attributes that need injection.</summary>
    public InjectableProperty[] Properties { get; init; } = [];

    /// <summary>
    /// Constructs a TestMetadata&lt;T&gt; from this entry's data and delegates.
    /// </summary>
    internal TestMetadata<T> ToTestMetadata(string testSessionId)
    {
        return new TestMetadata<T>
        {
            TestName = MethodName,
            TestClassType = typeof(T),
            TestMethodName = MethodName,
            Dependencies = Dependencies,
            DataSources = TestDataSources,
            ClassDataSources = ClassDataSources,
            PropertyDataSources = BuildPropertyDataSources(),
            PropertyInjections = BuildPropertyInjections(),
            InstanceFactory = CreateInstance,
            ClassInvoker = InvokeBody,
            InvokeMethodIndex = MethodIndex,
            ClassAttributeFactory = CreateAttributes,
            AttributeGroupIndex = AttributeGroupIndex,
            AttributeFactory = () => CreateAttributes(AttributeGroupIndex),
            FilePath = FilePath,
            LineNumber = LineNumber,
            MethodMetadata = MethodMetadata,
            RepeatCount = RepeatCount > 0 ? RepeatCount : null,
            TestSessionId = testSessionId,
        };
    }

    private PropertyDataSource[] BuildPropertyDataSources()
    {
        if (Properties.Length == 0) return [];
        var result = new PropertyDataSource[Properties.Length];
        for (var i = 0; i < Properties.Length; i++)
        {
            result[i] = new PropertyDataSource
            {
                PropertyName = Properties[i].Name,
                PropertyType = Properties[i].Type,
                DataSource = Properties[i].DataSource,
            };
        }
        return result;
    }

    private PropertyInjectionData[] BuildPropertyInjections()
    {
        if (Properties.Length == 0) return [];
        var result = new PropertyInjectionData[Properties.Length];
        for (var i = 0; i < Properties.Length; i++)
        {
            var prop = Properties[i];
            result[i] = new PropertyInjectionData
            {
                PropertyName = prop.Name,
                PropertyType = prop.Type,
                Setter = prop.SetValue,
                ValueFactory = static () => null,
            };
        }
        return result;
    }
}
