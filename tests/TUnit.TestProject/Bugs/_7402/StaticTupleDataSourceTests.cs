using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7402;

[EngineTest(ExpectedResult.Pass)]
public class StaticTupleDataSourceTests
{
    public static Tuple<string?> Value() => Tuple.Create<string?>(null);
    public static IEnumerable<Tuple<string?>> Rows() => [Value()];
    public static Task<Tuple<string?>> TaskRow() => Task.FromResult(Value());
    public static ValueTask<Tuple<string?>> ValueTaskRow() => new(Value());
    public static async IAsyncEnumerable<Tuple<string?>> AsyncRows()
    {
        await Task.Yield();
        yield return Value();
    }

    [Test]
    [MethodDataSource(nameof(Value))]
    [MethodDataSource(nameof(Rows))]
    [MethodDataSource(nameof(TaskRow))]
    [MethodDataSource(nameof(ValueTaskRow))]
    [MethodDataSource(nameof(AsyncRows))]
    public async Task Tuple_Holding_Null_Stays_A_Tuple(Tuple<string?>? value)
    {
        await Assert.That(value).IsNotNull();
        await Assert.That(value!.Item1).IsNull();
    }
}
