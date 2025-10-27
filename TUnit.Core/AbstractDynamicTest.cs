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

    /// <summary>
    /// The file path where the dynamic test was created
    /// </summary>
    public string? CreatorFilePath { get; set; }

    /// <summary>
    /// The line number where the dynamic test was created
    /// </summary>
    public int? CreatorLineNumber { get; set; }
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
    /// Parent test ID for property-based testing shrink relationships.
    /// Set to the original test ID when creating shrunk test variants.
    /// </summary>
    public Guid? ParentTestId { get; set; }

    /// <summary>
    /// Object bag for storing arbitrary data with the dynamic test.
    /// Used for passing property test metadata to shrunk tests.
    /// </summary>
    public Dictionary<string, object?> ObjectBag { get; set; } = new();

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
            CreatorLineNumber = CreatorLineNumber
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
