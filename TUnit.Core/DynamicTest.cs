using System.Linq.Expressions;

namespace TUnit.Core;

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
    public Type? TestClassType { get; set; }
}

public abstract class DynamicTest
{
    public abstract IEnumerable<DiscoveryResult> GetTests();
}

public abstract class DynamicTest<T> : DynamicTest where T : class;

public class DynamicTestInstance<T> : DynamicTest<T> where T : class
{
    public Expression<Action<T>>? TestMethod { get; set; }
    public object?[]? TestClassArguments { get; set; }
    public object?[]? TestMethodArguments { get; set; }
    public List<Attribute> Attributes { get; set; } =
    [
    ];

    public override IEnumerable<DiscoveryResult> GetTests()
    {
        // Create a discovery result for this dynamic test
        var result = new DynamicDiscoveryResult
        {
            TestMethod = TestMethod,
            TestClassArguments = TestClassArguments,
            TestMethodArguments = TestMethodArguments,
            Attributes = Attributes,
            TestClassType = typeof(T)
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
    IReadOnlyList<DynamicTest> CollectDynamicTests(string sessionId);
}

public class FailedDynamicTest<T> : DynamicTest where T : class
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
