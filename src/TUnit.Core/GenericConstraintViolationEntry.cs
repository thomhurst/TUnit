using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// A test method on a generic class whose class type arguments, inferred from class-level rows,
/// <see cref="GenerateGenericTestAttribute"/> or method-level rows, break the class's generic
/// constraints. A <see cref="TestEntry{T}"/> cannot be emitted for a closed type that breaks its own
/// constraints, so the source generator emits this entry against the open generic type instead. The
/// engine reports one failed test per message, with the same name and message as reflection mode.
/// </summary>
/// <remarks>
/// This type is used by generated code. It is not meant to be used directly.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class GenericConstraintViolationEntry
{
    /// <summary>Test method name.</summary>
    public required string MethodName { get; init; }

    /// <summary>Source file path.</summary>
    public required string FilePath { get; init; }

    /// <summary>Source start line number.</summary>
    public required int LineNumber { get; init; }

    /// <summary>Method metadata, declared on the open generic type.</summary>
    public required MethodMetadata MethodMetadata { get; init; }

    /// <summary>
    /// One message per invalid row or type, in the order reflection mode discovers them:
    /// class-level <see cref="GenerateGenericTestAttribute"/> types, then class rows, then method rows.
    /// </summary>
    public required string[] Messages { get; init; }

    internal TestMetadata ToTestMetadata([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type genericTypeDefinition, string testSessionId)
    {
        var metadata = GenericConstraintViolationTestMetadata.Create(
            genericTypeDefinition,
            MethodName,
            MethodMetadata,
            FilePath,
            LineNumber,
            Messages);
        metadata.TestSessionId = testSessionId;
        return metadata;
    }
}

/// <summary>
/// Test metadata that only reports generic constraint violations. Its method data sources are
/// <see cref="GenericConstraintViolationDataSource"/> instances, so each of their rows becomes one
/// failed test through the engine's data-generation-error path. Both engine modes build failures
/// for constraint-violating class type arguments through this type, so the results match.
/// </summary>
internal sealed class GenericConstraintViolationTestMetadata : TestMetadata
{
    public static GenericConstraintViolationTestMetadata Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type testClassType,
        string methodName,
        MethodMetadata methodMetadata,
        string filePath,
        int lineNumber,
        IReadOnlyList<string> messages)
    {
        var dataSources = new IDataSourceAttribute[messages.Count];
        for (var i = 0; i < dataSources.Length; i++)
        {
            dataSources[i] = GenericConstraintViolationDataSource.Create(messages[i]);
        }

        return Create(testClassType, methodName, methodMetadata, filePath, lineNumber, dataSources);
    }

    /// <summary>
    /// Creates the metadata over <paramref name="dataSources"/>, which are
    /// <see cref="GenericConstraintViolationDataSource"/> instances, some of them wrapping the row
    /// whose inferred type arguments break the constraints.
    /// </summary>
    public static GenericConstraintViolationTestMetadata Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type testClassType,
        string methodName,
        MethodMetadata methodMetadata,
        string filePath,
        int lineNumber,
        IDataSourceAttribute[] dataSources)
    {
        return new GenericConstraintViolationTestMetadata
        {
            TestName = methodName,
            TestClassType = testClassType,
            TestMethodName = methodName,
            FilePath = filePath,
            LineNumber = lineNumber,
            MethodMetadata = methodMetadata,
            AttributeFactory = static () => [],
            DataSources = dataSources,
            ClassDataSources = [],
            PropertyDataSources = [],
        };
    }

    // Every row of the data sources fails test building, so an executable test is never created.
    public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
        => static (_, metadata) => throw new InvalidOperationException(
            $"Test '{metadata.TestName}' on '{metadata.TestClassType.Name}' only reports generic constraint violations and cannot be executed.");
}

/// <summary>
/// Exposes the <see cref="GenericConstraintViolationEntry"/> instances registered for one open
/// generic type to the engine, alongside the regular <see cref="TestEntrySource{T}"/> sources.
/// </summary>
internal sealed class GenericConstraintViolationEntrySource : ITestEntrySource
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    private readonly Type _genericTypeDefinition;
    private readonly object _lock = new();
    private List<Func<GenericConstraintViolationEntry[]>>? _factories;
    private volatile GenericConstraintViolationEntry[]? _entries;
    private string? _className;

    public GenericConstraintViolationEntrySource([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type genericTypeDefinition, Func<GenericConstraintViolationEntry[]> factory)
    {
        _genericTypeDefinition = genericTypeDefinition;
        _factories = [factory];
    }

    internal void AddFactory(Func<GenericConstraintViolationEntry[]> factory)
    {
        lock (_lock)
        {
            if (_entries is not null)
            {
                _entries = [.. _entries, .. factory()];
                return;
            }

            _factories!.Add(factory);
        }
    }

    private GenericConstraintViolationEntry[] Resolve()
    {
        if (_entries is not null)
        {
            return _entries;
        }

        lock (_lock)
        {
            if (_entries is not null)
            {
                return _entries;
            }

            var entries = new List<GenericConstraintViolationEntry>();
            foreach (var factory in _factories!)
            {
                entries.AddRange(factory());
            }

            _entries = entries.ToArray();
            _factories = null;
            return _entries;
        }
    }

    public int Count => Resolve().Length;

    public Type ClassType => _genericTypeDefinition;

    public string ClassName => _className ??= Extensions.TestContextExtensions.GetNestedTypeName(_genericTypeDefinition);

    public TestEntryFilterData GetFilterData(int index)
    {
        var entry = Resolve()[index];
        return new TestEntryFilterData
        {
            MethodName = entry.MethodName,
            ClassName = ClassName,
            Categories = [],
            Properties = [],
            DependsOn = [],
            HasDataSource = true,
            RepeatCount = 0,
        };
    }

    public IReadOnlyList<TestMetadata> Materialize(int index, string testSessionId)
    {
        return [Resolve()[index].ToTestMetadata(_genericTypeDefinition, testSessionId)];
    }
}
