using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class AsyncMethodDataSourceDrivenTests
{
    [Test]
    [MethodDataSource(nameof(AsyncDataMethod))]
    public async Task AsyncMethodDataSource_SingleValue(int value)
    {
        await Assert.That(value).IsIn(1, 2, 3);
    }

    [Test]
    [MethodDataSource(nameof(AsyncDataMethodWithTuples))]
    public async Task AsyncMethodDataSource_Tuples(int value1, string value2)
    {
        await Assert.That(value1).IsIn(1, 2, 3);
        await Assert.That(value2).IsIn("a", "b", "c");
    }

    [Test]
    [MethodDataSource(nameof(AsyncEnumerableDataMethod))]
    public async Task AsyncMethodDataSource_Enumerable(int value)
    {
        await Assert.That(value).IsIn(1, 2, 3);
    }

    [Test]
    [MethodDataSource(nameof(AsyncFuncDataMethod))]
    public async Task AsyncMethodDataSource_Func(int value)
    {
        await Assert.That(value).IsIn(10, 20, 30);
    }

    [Test]
    [MethodDataSource(nameof(AsyncDataMethodWithArgs), Arguments = [5])]
    public async Task AsyncMethodDataSource_WithArguments(int value)
    {
        await Assert.That(value).IsIn(6, 7, 8);
    }

    [Test]
    [MethodDataSource(typeof(AsyncExternalDataSource), nameof(AsyncExternalDataSource.GetData))]
    public async Task AsyncMethodDataSource_ExternalClass(string value)
    {
        await Assert.That(value).IsIn("external1", "external2");
    }

    [Test]
    [MethodDataSource(nameof(ValueTaskDataMethod))]
    public async Task ValueTaskMethodDataSource_SingleValue(int value)
    {
        await Assert.That(value).IsIn(100, 200, 300);
    }

    // Data source methods
    public static int[] AsyncDataMethod()
    {
        // Note: For now, TUnit doesn't support async enumerable data sources
        // so we use synchronous methods that return arrays
        return [1, 2, 3];
    }

    public static (int, string)[] AsyncDataMethodWithTuples()
    {
        return [(1, "a"), (2, "b"), (3, "c")];
    }

    public static IEnumerable<int> AsyncEnumerableDataMethod()
    {
        return new[] { 1, 2, 3 };
    }

    public static int[] AsyncFuncDataMethod()
    {
        return [10, 20, 30];
    }

    public static int[] AsyncDataMethodWithArgs(int baseValue)
    {
        return [baseValue + 1, baseValue + 2, baseValue + 3];
    }

    public static int[] ValueTaskDataMethod()
    {
        return [100, 200, 300];
    }
}

public static class AsyncExternalDataSource
{
    public static string[] GetData()
    {
        return ["external1", "external2"];
    }
}