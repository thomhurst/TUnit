using System.Runtime.CompilerServices;

namespace TUnit.TestProject.Bugs._7617;

public class ConvertibleOptionalRowTests
{
    public sealed class Row
    {
        public static implicit operator (int, int)(Row value) => (25, 26);
    }

    public static IEnumerable<Row> Rows() => [new Row()];
    public static Row Direct() => new();
    public static Func<Row> Factory() => () => new();

    [Test]
    [MethodDataSource(nameof(Rows))]
    [MethodDataSource(nameof(Direct))]
    [MethodDataSource(nameof(Factory))]
    public async Task Converts_With_Optional_Tail((int, int) pair, int extra = 5, string text = "tail")
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(extra).IsEqualTo(5);
        await Assert.That(text).IsEqualTo("tail");
    }
    [Test]
    [MethodDataSource(nameof(Rows))]
    [MethodDataSource(nameof(Direct))]
    [MethodDataSource(nameof(Factory))]
    public async Task Converts_With_Params_Tail((int, int) pair, params int[] rest)
    {
        await Assert.That(pair).IsEqualTo((25, 26));
        await Assert.That(rest.Length).IsEqualTo(0);
    }

    public sealed class TupleRow : ITuple
    {
        public int Length => 2;
        public object this[int index] => index + 25;
    }

    public static IEnumerable<TupleRow> CustomTupleRows() => [new TupleRow()];
    public static IEnumerable<ITuple> InterfaceTupleRows() => [(25, 26)];

    [Test]
    [MethodDataSource(nameof(CustomTupleRows))]
    public async Task Custom_Tuple_Binds_Whole(TupleRow row, int extra = 5)
    {
        await Assert.That(row.Length).IsEqualTo(2);
        await Assert.That(extra).IsEqualTo(5);
    }

    [Test]
    [MethodDataSource(nameof(CustomTupleRows))]
    [MethodDataSource(nameof(InterfaceTupleRows))]
    public async Task Interface_Tuple_Binds_Whole(ITuple row, params int[] rest)
    {
        await Assert.That(row.Length).IsEqualTo(2);
        await Assert.That(rest.Length).IsEqualTo(0);
    }
}
