using TUnit.Assertions;
using TUnit.Assertions.Extensions;
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
    public static async Task<int[]> AsyncDataMethod()
    {
        await Task.Delay(10); // Simulate async work
        return [1, 2, 3];
    }

    public static async Task<(int, string)[]> AsyncDataMethodWithTuples()
    {
        await Task.Delay(10);
        return [(1, "a"), (2, "b"), (3, "c")];
    }

    public static async Task<IEnumerable<int>> AsyncEnumerableDataMethod()
    {
        await Task.Delay(10);
        return new[] { 1, 2, 3 };
    }

    public static async Task<Func<int>[]> AsyncFuncDataMethod()
    {
        await Task.Delay(10);
        return [() => 10, () => 20, () => 30];
    }

    public static async Task<int[]> AsyncDataMethodWithArgs(int baseValue)
    {
        await Task.Delay(10);
        return [baseValue + 1, baseValue + 2, baseValue + 3];
    }

    public static async ValueTask<int[]> ValueTaskDataMethod()
    {
        await Task.Delay(10);
        return [100, 200, 300];
    }
}

public static class AsyncExternalDataSource
{
    public static async Task<string[]> GetData()
    {
        await Task.Delay(10);
        return ["external1", "external2"];
    }
}