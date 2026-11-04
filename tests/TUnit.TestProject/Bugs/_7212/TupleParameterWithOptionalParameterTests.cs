using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7212;

/// <summary>
/// A row whose single value is a tuple of the first data parameter's type binds to that parameter
/// whole, and the remaining optional or params parameters take their defaults, in both
/// source-generated and reflection mode, for object[] and typed sources (#7212, #7340).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class TupleParameterWithOptionalParameterTests
{
    public static IEnumerable<object[]> TupleRows()
    {
        yield return [(25, 26)];
    }

    public static IEnumerable<(int, int)> TypedTupleRows()
    {
        yield return (25, 26);
    }

    public static Task<IEnumerable<(int, int)>> TaskTupleRows() => Task.FromResult(TypedTupleRows());

    public static ValueTask<IEnumerable<(int, int)>> ValueTaskTupleRows() => new(TypedTupleRows());

    public static async IAsyncEnumerable<(int, int)> AsyncTupleRows()
    {
        await Task.Yield();
        yield return (25, 26);
    }

    public IEnumerable<(int, int)> InstanceTupleRows() => TypedTupleRows();

    public static IEnumerable<object[]> TupleAndValueRows()
    {
        yield return [(25, 26), 7];
    }

    public static IEnumerable<((int, int), int)> NestedTupleRows()
    {
        yield return ((25, 26), 7);
    }

    [Test]
    [MethodDataSource(nameof(TupleRows))]
    [MethodDataSource(nameof(TypedTupleRows))]
    [MethodDataSource(nameof(TaskTupleRows))]
    [MethodDataSource(nameof(ValueTaskTupleRows))]
    [MethodDataSource(nameof(AsyncTupleRows))]
    [MethodDataSource(nameof(InstanceTupleRows))]
    public async Task Tuple_And_Optional((int, int) pair, int extra = 5)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(5);
    }

    [Test]
    [MethodDataSource(nameof(TupleRows))]
    [MethodDataSource(nameof(TypedTupleRows))]
    public async Task Tuple_And_Two_Optionals((int, int) pair, int extra = 5, string name = "default")
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(5);
        await Assert.That(name).IsEqualTo("default");
    }

    [Test]
    [MethodDataSource(nameof(TupleRows))]
    [MethodDataSource(nameof(TypedTupleRows))]
    public async Task Tuple_Optional_And_Token((int, int) pair, int extra = 5, CancellationToken token = default)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(5);
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(TupleRows))]
    [MethodDataSource(nameof(TypedTupleRows))]
    public async Task Token_Tuple_And_Optional(CancellationToken token, (int, int) pair, int extra = 5)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(5);
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(TupleRows))]
    [MethodDataSource(nameof(TypedTupleRows))]
    public async Task Tuple_And_Params((int, int) pair, params int[] rest)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(rest).IsEmpty();
    }

    [Test]
    [MethodDataSource(nameof(TupleRows))]
    [MethodDataSource(nameof(TypedTupleRows))]
    public async Task Tuple_And_Params_Collection((int, int) pair, params IEnumerable<int> rest)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(rest).IsEmpty();
    }

    [Test]
    [MethodDataSource(nameof(TupleAndValueRows))]
    public async Task Tuple_And_Supplied_Optional((int, int) pair, int extra = 5)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(7);
    }

    [Test]
    [MethodDataSource(nameof(NestedTupleRows))]
    public async Task Nested_Tuple_Row_Is_Spread((int, int) pair, int extra = 5)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(7);
    }
}
