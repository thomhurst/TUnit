using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Shared factory for <see cref="TestEntry{T}"/> invoked by source-generated test sources.
/// </summary>
/// <remarks>
/// A single generic factory call compiles to far less IL at each generated call site than the
/// equivalent ~17-property object initializer (one call token versus a newobj plus a member
/// token per property), and the initializer body itself is JIT-compiled once here instead of
/// once per generated test class. Array parameters default to <c>null</c> so generated code can
/// omit them entirely when empty.
/// </remarks>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public static class TestEntryFactory
{
    public static TestEntry<T> Create<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        string methodName,
        string fullyQualifiedName,
        string filePath,
        int lineNumber,
        MethodMetadata methodMetadata,
        Func<Type[], object?[], T> createInstance,
        Func<T, int, object?[], CancellationToken, ValueTask> invokeBody,
        int methodIndex,
        Func<int, Attribute[]> createAttributes,
        int attributeGroupIndex,
        int startColumnNumber = 0,
        int endLineNumber = 0,
        int endColumnNumber = 0,
        bool hasDataSource = false,
        int repeatCount = 0,
        string[]? categories = null,
        string[]? properties = null,
        string[]? dependsOn = null,
        TestDependency[]? dependencies = null,
        IDataSourceAttribute[]? testDataSources = null,
        IDataSourceAttribute[]? classDataSources = null,
        InjectableProperty[]? injectableProperties = null) where T : class
    {
        return new TestEntry<T>
        {
            MethodName = methodName,
            FullyQualifiedName = fullyQualifiedName,
            FilePath = filePath,
            LineNumber = lineNumber,
            StartColumnNumber = startColumnNumber,
            EndLineNumber = endLineNumber,
            EndColumnNumber = endColumnNumber,
            Categories = categories ?? [],
            Properties = properties ?? [],
            DependsOn = dependsOn ?? [],
            Dependencies = dependencies ?? [],
            HasDataSource = hasDataSource,
            RepeatCount = repeatCount,
            MethodMetadata = methodMetadata,
            CreateInstance = createInstance,
            InvokeBody = invokeBody,
            MethodIndex = methodIndex,
            CreateAttributes = createAttributes,
            AttributeGroupIndex = attributeGroupIndex,
            TestDataSources = testDataSources ?? [],
            ClassDataSources = classDataSources ?? [],
            InjectableProperties = injectableProperties ?? [],
        };
    }

    /// <summary>
    /// Creates a <see cref="TestEntry{T}"/> together with its <see cref="MethodMetadata"/>.
    /// </summary>
    /// <remarks>
    /// Building the method metadata here rather than at each generated call site keeps the
    /// generated static constructor small: every test otherwise needs its own static field plus a
    /// separate metadata factory call, and that IL is JIT-compiled once per test class at startup.
    /// <paramref name="returnType"/> defaults to <see cref="void"/>.
    /// </remarks>
    public static TestEntry<T> CreateWithClassMetadata<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        string methodName,
        string fullyQualifiedName,
        string filePath,
        int lineNumber,
        ClassMetadata classMetadata,
        Func<Type[], object?[], T> createInstance,
        Func<T, int, object?[], CancellationToken, ValueTask> invokeBody,
        int methodIndex,
        Func<int, Attribute[]> createAttributes,
        int attributeGroupIndex,
        Type? returnType = null,
        int genericTypeCount = 0,
        ParameterMetadata[]? parameters = null,
        int startColumnNumber = 0,
        int endLineNumber = 0,
        int endColumnNumber = 0,
        bool hasDataSource = false,
        int repeatCount = 0,
        string[]? categories = null,
        string[]? properties = null,
        string[]? dependsOn = null,
        TestDependency[]? dependencies = null,
        IDataSourceAttribute[]? testDataSources = null,
        IDataSourceAttribute[]? classDataSources = null,
        InjectableProperty[]? injectableProperties = null) where T : class
    {
        var methodMetadata = MethodMetadataFactory.Create(
            methodName,
            typeof(T),
            returnType ?? typeof(void),
            classMetadata,
            genericTypeCount,
            parameters);

        return Create(
            methodName,
            fullyQualifiedName,
            filePath,
            lineNumber,
            methodMetadata,
            createInstance,
            invokeBody,
            methodIndex,
            createAttributes,
            attributeGroupIndex,
            startColumnNumber,
            endLineNumber,
            endColumnNumber,
            hasDataSource,
            repeatCount,
            categories,
            properties,
            dependsOn,
            dependencies,
            testDataSources,
            classDataSources,
            injectableProperties);
    }
}
