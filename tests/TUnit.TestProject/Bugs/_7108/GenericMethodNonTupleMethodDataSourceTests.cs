using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7108;

/// <summary>
/// https://github.com/thomhurst/TUnit/issues/7108: source-gen mode only inferred a generic test
/// method's type arguments from <c>IEnumerable&lt;Func&lt;(...)&gt;&gt;</c> data sources, so every other
/// row shape dropped the test silently. Each test here must be discovered in both modes.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class GenericMethodNonTupleMethodDataSourceTests
{
    public static IEnumerable<Func<int>> FuncRows()
    {
        yield return () => 1;
    }

    public static IEnumerable<int> PlainRows()
    {
        yield return 2;
    }

    public static string[] ArrayRows() => ["three"];

    public static async IAsyncEnumerable<int> AsyncRows()
    {
        await Task.Yield();
        yield return 4;
    }

    public static Task<IEnumerable<int>> TaskRows() => Task.FromResult<IEnumerable<int>>([5]);

    public static IEnumerable<TestDataRow<int>> TestDataRows()
    {
        yield return new TestDataRow<int>(6);
    }

    public static IEnumerable<List<int>> ListRows()
    {
        yield return [7];
    }

    public static IEnumerable<(int, string)> PlainTupleRows()
    {
        yield return (8, "eight");
    }

    public static IEnumerable<int> RowsWithArgument(int value)
    {
        yield return value;
    }

    [Test]
    [MethodDataSource(nameof(FuncRows))]
    public async Task Func_Row<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)1);
    }

    [Test]
    [MethodDataSource(nameof(PlainRows))]
    public async Task Plain_Row<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)2);
    }

    [Test]
    [MethodDataSource(nameof(ArrayRows))]
    public async Task Array_Row<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(value).IsEqualTo((T)(object)"three");
    }

    [Test]
    [MethodDataSource(nameof(AsyncRows))]
    public async Task Async_Enumerable_Row<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)4);
    }

    [Test]
    [MethodDataSource(nameof(TaskRows))]
    public async Task Task_Row<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)5);
    }

    [Test]
    [MethodDataSource(nameof(TestDataRows))]
    public async Task Test_Data_Row<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)6);
    }

    [Test]
    [MethodDataSource(nameof(ListRows))]
    public async Task Nested_Generic_Row<T>(List<T> value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value.Count).IsEqualTo(1);
    }

    [Test]
    [MethodDataSource(nameof(PlainTupleRows))]
    public async Task Plain_Tuple_Row<T1, T2>(T1 number, T2 text)
    {
        await Assert.That(typeof(T1)).IsEqualTo(typeof(int));
        await Assert.That(typeof(T2)).IsEqualTo(typeof(string));
        await Assert.That(number).IsEqualTo((T1)(object)8);
        await Assert.That(text).IsEqualTo((T2)(object)"eight");
    }

    [Test]
    [MethodDataSource(nameof(RowsWithArgument), Arguments = [9])]
    public async Task Data_Method_With_Arguments<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)9);
    }
}
