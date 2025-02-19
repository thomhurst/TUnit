using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public record TestDetails<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
TClassType
> : TestDetails where TClassType : class
{
    [JsonIgnore]
    public required ResettableLazy<TClassType> LazyClassInstance { get; init; }

    public override object ClassInstance => LazyClassInstance.Value;
} 

public abstract record TestDetails
{
    public required string TestId { get; init; }
    
    public required string TestName { get; init; }

    [field: AllowNull, MaybeNull]
    public SourceGeneratedClassInformation TestClass => field ??= TestMethod.Class;

    [field: AllowNull, MaybeNull]
    public Type[] TestMethodParameterTypes => field ??= TestMethod.Parameters.Select(x => x.Type).ToArray();
    public required object?[] TestMethodArguments { get; init; }

    [field: AllowNull, MaybeNull]
    public Type[] TestClassParameterTypes => field ??= TestMethod.Class.Parameters.Select(x => x.Type).ToArray();
    public required object?[] TestClassArguments { get; init; }
    public required object?[] TestClassInjectedPropertyArguments { get; init; }

    internal readonly List<string> MutableCategories = [];
    public IReadOnlyList<string> Categories => MutableCategories;
    public required SourceGeneratedMethodInformation TestMethod { get; init; }
    public abstract object? ClassInstance { get; }
    public required int CurrentRepeatAttempt { get; init; }
    public required int RepeatLimit { get; init; }
    public int RetryLimit { get; internal set; }

    public TimeSpan? Timeout { get; internal set; }
    
    public IParallelConstraint? ParallelConstraint { get; internal set; }
    public IReadOnlyDictionary<string, string> CustomProperties => InternalCustomProperties;
    internal Dictionary<string, string> InternalCustomProperties { get; } = [];

    [JsonIgnore] public Attribute[] AssemblyAttributes => TestClass.Assembly.Attributes;

    [JsonIgnore] public Attribute[] ClassAttributes => TestClass.Attributes;

    [JsonIgnore] public Attribute[] TestAttributes => TestMethod.Attributes;

    [JsonIgnore]
    public required Attribute[] DataAttributes { get; init; }

    [JsonIgnore]
    [field: AllowNull, MaybeNull]
    public Attribute[] Attributes => field ??= [..TestAttributes, ..ClassAttributes, ..AssemblyAttributes];

    [JsonIgnore]
    internal Func<TestContext, Exception, int, Task<bool>>? RetryLogic { get; set; }
    
    public required Type ReturnType { get; init; }

    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }
    internal string? DisplayName { get; set; }
    
    public IParallelLimit? ParallelLimit { get; internal set; }


    internal bool IsSameTest(TestDetails testDetails) => TestName == testDetails.TestName &&
                                                                 TestClass == testDetails.TestClass &&
                                                                 TestMethodParameterTypes.SequenceEqual(testDetails.TestMethodParameterTypes);
}