using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Factory for creating TestMetadata instances.
/// A single generic method <c>Create&lt;T&gt;() where T : class</c> gets JIT'd once by the .NET runtime
/// (reference types share native code), so replacing 1,000 inline object initializers with calls
/// to this shared factory dramatically reduces JIT-compiled native code size.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TestMetadataFactory
{
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2091",
        Justification = "Factory is only called from generated code that always passes concrete types")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2087",
        Justification = "Factory is only called from generated code that always passes concrete types")]
    public static TestMetadata<T> Create<T>(
        string testName,
        string testMethodName,
        int lineNumber,
        Func<T, object?[], CancellationToken, ValueTask>? invokeTypedTest,
        Func<Attribute[]>? attributeFactory,
        Func<Type[], object?[], T> instanceFactory,
        MethodMetadata methodMetadata,
        string testSessionId,
        string filePath = "",
        int inheritanceDepth = 0,
        TestDependency[]? dependencies = null,
        IDataSourceAttribute[]? dataSources = null,
        IDataSourceAttribute[]? classDataSources = null,
        PropertyDataSource[]? propertyDataSources = null,
        PropertyInjectionData[]? propertyInjections = null,
        int? repeatCount = null,
        Func<T, int, object?[], CancellationToken, ValueTask>? classInvoker = null,
        int invokeMethodIndex = -1,
        Func<int, Attribute[]>? classAttributeFactory = null,
        int attributeGroupIndex = -1
    ) where T : class
    {
        // Resolve attribute factory: use class-level consolidated factory when available
        Func<Attribute[]> resolvedAttributeFactory = attributeFactory
            ?? (classAttributeFactory != null && attributeGroupIndex >= 0
                ? () => classAttributeFactory(attributeGroupIndex)
                : static () => []);

        return new TestMetadata<T>
        {
            TestName = testName,
            TestClassType = typeof(T),
            TestMethodName = testMethodName,
            Dependencies = dependencies ?? [],
            AttributeFactory = resolvedAttributeFactory,
            DataSources = dataSources ?? [],
            ClassDataSources = classDataSources ?? [],
            PropertyDataSources = propertyDataSources ?? [],
            PropertyInjections = propertyInjections ?? [],
            InheritanceDepth = inheritanceDepth,
            FilePath = filePath,
            LineNumber = lineNumber,
            MethodMetadata = methodMetadata,
            InstanceFactory = instanceFactory,
            InvokeTypedTest = invokeTypedTest,
            ClassInvoker = classInvoker,
            InvokeMethodIndex = invokeMethodIndex,
            ClassAttributeFactory = classAttributeFactory,
            AttributeGroupIndex = attributeGroupIndex,
            RepeatCount = repeatCount,
            TestSessionId = testSessionId,
        };
    }
}
