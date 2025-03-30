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
    public SourceGeneratedClassInformation TestClass => field ??= TestMethod.Class;

    /// <summary>
    /// Gets the parameter types for the test method.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public Type[] TestMethodParameterTypes => field ??= TestMethod.Parameters.Select(x => x.Type).ToArray();

    /// <summary>
    /// Gets or sets the arguments for the test method.
    /// </summary>
    public required object?[] TestMethodArguments { get; init; }

    /// <summary>
    /// Gets the parameter types for the test class.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public Type[] TestClassParameterTypes => field ??= TestMethod.Class.Parameters.Select(x => x.Type).ToArray();

    /// <summary>
    /// Gets or sets the arguments for the test class.
    /// </summary>
    public required object?[] TestClassArguments { get; init; }

    /// <summary>
    /// Gets or sets the injected property arguments for the test class.
    /// </summary>
    public required object?[] TestClassInjectedPropertyArguments { get; init; }

    internal readonly List<string> MutableCategories = [];

    /// <summary>
    /// Gets the categories for the test.
    /// </summary>
    public IReadOnlyList<string> Categories => MutableCategories;

    /// <summary>
    /// Gets or sets the test method information.
    /// </summary>
    public required SourceGeneratedMethodInformation TestMethod { get; init; }

    /// <summary>
    /// Gets the instance of the test class.
    /// </summary>
    public abstract object ClassInstance { get; }

    /// <summary>
    /// Gets or sets the current repeat attempt for the test.
    /// </summary>
    public required int CurrentRepeatAttempt { get; init; }

    /// <summary>
    /// Gets or sets the repeat limit for the test.
    /// </summary>
    public required int RepeatLimit { get; init; }

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
    public IReadOnlyDictionary<string, string> CustomProperties => InternalCustomProperties;

    internal Dictionary<string, string> InternalCustomProperties { get; } = [];

    /// <summary>
    /// Gets the attributes for the test assembly.
    /// </summary>
    [JsonIgnore] public Attribute[] AssemblyAttributes => TestClass.Assembly.Attributes;

    /// <summary>
    /// Gets the attributes for the test class.
    /// </summary>
    [JsonIgnore] public Attribute[] ClassAttributes => TestClass.Attributes;

    /// <summary>
    /// Gets the attributes for the test method.
    /// </summary>
    [JsonIgnore] public Attribute[] TestAttributes => TestMethod.Attributes;

    /// <summary>
    /// Gets the attributes for the test.
    /// </summary>
    [JsonIgnore]
    [field: AllowNull, MaybeNull]
    public Attribute[] Attributes => field ??= [..ExtraAttributes, ..TestAttributes, ..ClassAttributes, ..AssemblyAttributes, ..DataAttributes];

    [JsonIgnore] internal Attribute[] ExtraAttributes { get; init; } = [];
    
    /// <summary>
    /// Gets the attributes that specify the test data.
    /// </summary>
    [JsonIgnore]
    public required Attribute[] DataAttributes { get; init; }
    
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
                                                                 TestClass == testDetails.TestClass &&
                                                                 TestMethodParameterTypes.SequenceEqual(testDetails.TestMethodParameterTypes);
}