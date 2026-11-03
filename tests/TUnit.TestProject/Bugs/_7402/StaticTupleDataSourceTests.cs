using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7402;

[EngineTest(ExpectedResult.Pass)]
public class StaticTupleDataSourceTests
{
    public static (int, int) NumericValue() => (1, 2);
    public static IEnumerable<(int, int)> NumericRows() => [NumericValue()];
    public static Task<(int, int)> NumericTask() => Task.FromResult(NumericValue());
    public static ValueTask<(int, int)> NumericValueTask() => new(NumericValue());
    public static async IAsyncEnumerable<(int, int)> NumericAsyncRows()
    {
        await Task.Yield();
        yield return NumericValue();
    }

    public static (int, int, int, int, int, int, int, int) LongNumericValue() => (1, 2, 3, 4, 5, 6, 7, 8);

    [Test]
    [MethodDataSource(nameof(LongNumericValue))]
    public async Task Long_Tuple_Elements_Can_Be_Converted((long, long, long, long, long, long, long, long) value)
    {
        await Assert.That(value).IsEqualTo((1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L));
    }

    [Test]
    [MethodDataSource(nameof(NumericValue))]
    [MethodDataSource(nameof(NumericRows))]
    [MethodDataSource(nameof(NumericTask))]
    [MethodDataSource(nameof(NumericValueTask))]
    [MethodDataSource(nameof(NumericAsyncRows))]
    public async Task Tuple_Elements_Can_Be_Converted((long, long) value)
    {
        await Assert.That(value.Item1).IsEqualTo(1L);
        await Assert.That(value.Item2).IsEqualTo(2L);
    }

    [Test]
    [MethodDataSource(nameof(NumericValue))]
    public async Task Tuple_Conversion_With_Trailing_Token((long, long) value, CancellationToken token)
    {
        await Assert.That(value).IsEqualTo((1L, 2L));
        await Assert.That(token).IsEqualTo(TestContext.Current!.Execution.CancellationToken);
    }

    [Test]
    [MethodDataSource(nameof(NumericValue))]
    public async Task Tuple_Conversion_With_Leading_Token(CancellationToken token, (long, long) value)
    {
        await Assert.That(value).IsEqualTo((1L, 2L));
        await Assert.That(token).IsEqualTo(TestContext.Current!.Execution.CancellationToken);
    }

    public static Tuple<int, int, int, int, int, int, int, Tuple<int>> LongReferenceValue()
        => Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8);

    [Test]
#pragma warning disable TUnit0001 // Exercise runtime element conversion; reference-tuple widening is tracked in #7480.
    [MethodDataSource(nameof(LongReferenceValue))]
#pragma warning restore TUnit0001
    public async Task Long_Reference_Tuple_Elements_Can_Be_Converted(
        Tuple<long, long, long, long, long, long, long, Tuple<long>> value)
    {
        await Assert.That(value).IsEqualTo(Tuple.Create(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L));
    }

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
