using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Represents the details of a test.
/// </summary>
/// <typeparam name="TClassType">The type of the test class.</typeparam>
public record TestDetails<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClassType> : TestDetails where TClassType : class
{
    /// <summary>
    /// Gets or sets the lazy class instance.
    /// </summary>
    [JsonIgnore]
    public required ResettableLazy<TClassType> LazyClassInstance { get; init; }

    /// <inheritdoc />
    public override object ClassInstance => LazyClassInstance.Value;
}

/// <summary>
/// Represents the base details of a test.
/// </summary>
public abstract record TestDetails
{
    /// <summary>
    /// Gets or sets the test ID.
    /// </summary>
    public required string TestId { get; init; }

    /// <summary>
    /// Gets or sets the test name.
    /// </summary>
    public required string TestName { get; init; }

    /// <summary>
    /// Gets the class information for the test.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public ClassMetadata ClassMetadata => field ??= MethodMetadata.Class;

    /// <summary>
    /// Gets the parameter types for the test method.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public Type[] TestMethodParameterTypes => field ??= MethodMetadata.Parameters.Select(x => x.Type).ToArray();

    /// <summary>
    /// Gets or sets the arguments for the test method.
    /// </summary>
    public required object?[] TestMethodArguments { get; init; }

    /// <summary>
    /// Gets the parameter types for the test class.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public Type[] TestClassParameterTypes => field ??= MethodMetadata.Class.Parameters.Select(x => x.Type).ToArray();

    /// <summary>
    /// Gets or sets the arguments for the test class.
    /// </summary>
    public required object?[] TestClassArguments { get; init; }

    /// <summary>
    /// Gets or sets the injected property arguments for the test class.
    /// </summary>
    public required IDictionary<string, object?> TestClassInjectedPropertyArguments { get; init; }

    internal readonly List<string> MutableCategories = [];

    /// <summary>
    /// Gets the categories for the test.
    /// </summary>
    public IReadOnlyList<string> Categories => MutableCategories;

    /// <summary>
    /// Gets or sets the test method information.
    /// </summary>
    public required MethodMetadata MethodMetadata { get; init; }

    /// <summary>
    /// Gets the test method information (alias for MethodMetadata).
    /// </summary>
    public MethodMetadata TestMethod => MethodMetadata;

    /// <summary>
    /// Gets the test class information.
    /// </summary>
    public ClassMetadata TestClass => ClassMetadata;

    /// <summary>
    /// Gets the instance of the test class.
    /// </summary>
    public abstract object ClassInstance { get; }

    /// <summary>
    /// Gets or sets the retry limit for the test.
    /// </summary>
    public int RetryLimit { get; internal set; }

    /// <summary>
    /// Gets or sets the timeout for the test.
    /// </summary>
    public TimeSpan? Timeout { get; internal set; }

    /// <summary>
    /// Gets or sets the parallel constraint for the test.
    /// </summary>
    public IParallelConstraint? ParallelConstraint { get; internal set; }

    /// <summary>
    /// Gets the custom properties for the test.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public IReadOnlyDictionary<string, IReadOnlyList<string>> CustomProperties => field ??= InternalCustomProperties.ToDictionary(
        kvp => kvp.Key, IReadOnlyList<string> (kvp) => kvp.Value.AsReadOnly()
    );

    internal ConcurrentDictionary<string, List<string>> InternalCustomProperties { get; } = [];

    /// <summary>
    /// Gets the attributes for the test assembly.
    /// </summary>
    [JsonIgnore]
    public AttributeMetadata[] AssemblyAttributes => ClassMetadata.Assembly.Attributes;

    /// <summary>
    /// Gets the attributes for the test class.
    /// </summary>
    [JsonIgnore]
    public AttributeMetadata[] ClassAttributes => ClassMetadata.Attributes;

    /// <summary>
    /// Gets the attributes for the test method.
    /// </summary>
    [JsonIgnore]
    public AttributeMetadata[] TestAttributes => MethodMetadata.Attributes;

    /// <summary>
    /// Gets all the attributes for the test (including dynamic, method, class, assembly, and data attributes).
    /// </summary>
    [JsonIgnore]
    [field: AllowNull, MaybeNull]
    public AttributeMetadata[] Attributes => field ??= [
        ..DynamicAttributes,
        ..TestAttributes,
        ..ClassAttributes,
        ..AssemblyAttributes,
        ..DataAttributes
    ];

    [JsonIgnore]
    [field: AllowNull, MaybeNull]
    public AttributeMetadata[] DynamicAttributes
    {
        get => field ??= ConvertToAttributeMetadata(
            _rawDynamicAttributes,
            TestAttributeTarget.Method,
            MethodMetadata.Name,
            MethodMetadata.Type);
        init
        {
            field = value;
            _rawDynamicAttributes = value?.Select(ta => ta.Instance).ToArray() ?? [];
        }
    }

    internal Attribute[] _rawDynamicAttributes = [];

    /// <summary>
    /// Gets the attributes that specify the test data.
    /// </summary>
    [JsonIgnore]
    [field: AllowNull, MaybeNull]
    public required AttributeMetadata[] DataAttributes
    {
        get => field ??= ConvertToAttributeMetadata(
            _rawDataAttributes,
            TestAttributeTarget.Method,
            MethodMetadata.Name,
            MethodMetadata.Type);
        init
        {
            field = value;
            _rawDataAttributes = value?.Select(ta => ta.Instance).ToArray() ?? [];
        }
    }

    internal Attribute[] _rawDataAttributes = [];

    /// <summary>
    /// Helper method to create TestDetails with raw Attribute arrays (for backward compatibility)
    /// </summary>
    public static TestDetails<TClassType> CreateWithRawAttributes<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClassType>(
        string testId,
        ResettableLazy<TClassType> lazyClassInstance,
        object?[] testClassArguments,
        object?[] testMethodArguments,
        IDictionary<string, object?> testClassInjectedPropertyArguments,
        MethodMetadata testMethod,
        string testName,
        Type returnType,
        string testFilePath,
        int testLineNumber,
        Attribute[] dynamicAttributes,
        Attribute[] dataAttributes) where TClassType : class
    {
        var details = new TestDetails<TClassType>
        {
            TestId = testId,
            LazyClassInstance = lazyClassInstance,
            TestClassArguments = testClassArguments,
            TestMethodArguments = testMethodArguments,
            TestClassInjectedPropertyArguments = testClassInjectedPropertyArguments,
            MethodMetadata = testMethod,
            TestName = testName,
            ReturnType = returnType,
            TestFilePath = testFilePath,
            TestLineNumber = testLineNumber,
            DynamicAttributes = [], // Will be set below
            DataAttributes = [] // Will be set below
        };

        // Set the raw attributes which will be converted to TestAttributeMetadata on access
        details._rawDynamicAttributes = dynamicAttributes;
        details._rawDataAttributes = dataAttributes;

        return details;
    }

    [JsonIgnore]
    internal Func<TestContext, Exception, int, Task<bool>>? RetryLogic { get; set; }

    /// <summary>
    /// Gets or sets the return type for the test.
    /// </summary>
    public required Type ReturnType { get; init; }

    /// <summary>
    /// Gets or sets the file path for the test.
    /// </summary>
    public required string TestFilePath { get; init; }

    /// <summary>
    /// Gets or sets the line number for the test.
    /// </summary>
    public required int TestLineNumber { get; init; }

    internal string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the parallel limit for the test.
    /// </summary>
    public IParallelLimit? ParallelLimit { get; internal set; }

    internal bool IsSameTest(TestDetails testDetails) => TestName == testDetails.TestName &&
                                                                 ClassMetadata == testDetails.ClassMetadata &&
                                                                 TestMethodParameterTypes.SequenceEqual(testDetails.TestMethodParameterTypes);

    private static AttributeMetadata[] ConvertToAttributeMetadata(
        Attribute[] attributes,
        TestAttributeTarget targetElement,
        string? targetMemberName = null,
        Type? targetType = null)
    {
        return attributes.Select(attr => new AttributeMetadata
        {
            Instance = attr,
            TargetElement = targetElement,
            TargetMemberName = targetMemberName,
            TargetType = targetType
        }).ToArray();
    }
}
