using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Factory for creating TestMetadata instances.
/// Used by the per-method source generation path (generic/inherited tests).
/// A single generic method gets JIT'd once by the .NET runtime (reference types
/// share native code), reducing JIT-compiled native code size.
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
        Func<T, object?[], CancellationToken, ValueTask> invokeTypedTest,
        Func<Attribute[]> attributeFactory,
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
        int? repeatCount = null
    ) where T : class
    {
        return new TestMetadata<T>
        {
            TestName = testName,
            TestClassType = typeof(T),
            TestMethodName = testMethodName,
            Dependencies = dependencies ?? [],
            AttributeFactory = attributeFactory,
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
            RepeatCount = repeatCount,
            TestSessionId = testSessionId,
        };
    }
}
