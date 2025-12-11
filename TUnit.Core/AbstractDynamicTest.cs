using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace TUnit.Core;

/// <summary>
/// Interface for dynamic tests that track where they were created
/// </summary>
public interface IDynamicTestCreatorLocation
{
    string? CreatorFilePath { get; set; }
    int? CreatorLineNumber { get; set; }
}

public class DiscoveryResult
{
    public static DiscoveryResult Empty => new();
}

public class DynamicDiscoveryResult : DiscoveryResult
{
    public Expression? TestMethod { get; set; }
    public object?[]? TestClassArguments { get; set; }
    public object?[]? TestMethodArguments { get; set; }
    public List<Attribute> Attributes { get; set; } =
    [
    ];

    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicFields
        | DynamicallyAccessedMemberTypes.NonPublicFields)]
    public Type? TestClassType { get; set; }

    public string? CreatorFilePath { get; set; }

    public int? CreatorLineNumber { get; set; }

    public string? ParentTestId { get; set; }

    public Enums.TestRelationship? Relationship { get; set; }

    public Dictionary<string, object?>? Properties { get; set; }

    public string? DisplayName { get; set; }

    private string? _uniqueId;

    /// <summary>
    /// Unique identifier for this dynamic test instance. 
    /// Auto-generated if not explicitly set. Used to ensure uniqueness when multiple tests call the same method.
    /// </summary>
    public string UniqueId
    {
        get => _uniqueId ??= Guid.NewGuid().ToString("N").Substring(0, 8);
        set => _uniqueId = value;
    }
}

public abstract class AbstractDynamicTest
{
    public abstract IEnumerable<DiscoveryResult> GetTests();
}

public abstract class AbstractDynamicTest<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.NonPublicConstructors
    | DynamicallyAccessedMemberTypes.PublicProperties
    | DynamicallyAccessedMemberTypes.PublicMethods
    | DynamicallyAccessedMemberTypes.NonPublicMethods
    | DynamicallyAccessedMemberTypes.PublicFields
    | DynamicallyAccessedMemberTypes.NonPublicFields)] T> : AbstractDynamicTest where T : class;

public class DynamicTest<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.NonPublicConstructors
    | DynamicallyAccessedMemberTypes.PublicProperties
    | DynamicallyAccessedMemberTypes.PublicMethods
    | DynamicallyAccessedMemberTypes.NonPublicMethods
    | DynamicallyAccessedMemberTypes.PublicFields
    | DynamicallyAccessedMemberTypes.NonPublicFields)]T> : AbstractDynamicTest<T>, IDynamicTestCreatorLocation where T : class
{
    public Expression<Action<T>>? TestMethod { get; set; }
    public object?[]? TestClassArguments { get; set; }
    public object?[]? TestMethodArguments { get; set; }
    public List<Attribute> Attributes { get; set; } =
    [
    ];

    /// <summary>
    /// The file path where this dynamic test was created
    /// </summary>
    public string? CreatorFilePath { get; set; }

    /// <summary>
    /// The line number where this dynamic test was created
    /// </summary>
    public int? CreatorLineNumber { get; set; }

    /// <summary>
    /// Optional display name for this test. If not provided, a default name will be generated.
    /// </summary>
    public string? DisplayName { get; set; }

    public override IEnumerable<DiscoveryResult> GetTests()
    {
        var result = new DynamicDiscoveryResult
        {
            TestMethod = TestMethod,
            TestClassArguments = TestClassArguments,
            TestMethodArguments = TestMethodArguments,
            Attributes = Attributes,
            TestClassType = typeof(T),
            CreatorFilePath = CreatorFilePath,
            CreatorLineNumber = CreatorLineNumber,
            DisplayName = DisplayName
        };

        yield return result;
    }
}

public static class DynamicTestHelper
{
    public static T Argument<T>() => default(T)!;
}

public interface IDynamicTestSource
{
    IReadOnlyList<AbstractDynamicTest> CollectDynamicTests(string sessionId);
}

public class FailedDynamicTest<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.NonPublicConstructors
    | DynamicallyAccessedMemberTypes.PublicProperties
    | DynamicallyAccessedMemberTypes.PublicMethods
    | DynamicallyAccessedMemberTypes.NonPublicMethods)] T> : AbstractDynamicTest where T : class
{
    public string TestId { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public Exception Exception { get; set; } = null!;
    public string TestFilePath { get; set; } = string.Empty;
    public int TestLineNumber { get; set; }

    public override IEnumerable<DiscoveryResult> GetTests()
    {
        // Failed dynamic tests don't produce any test results
        yield break;
    }
}
