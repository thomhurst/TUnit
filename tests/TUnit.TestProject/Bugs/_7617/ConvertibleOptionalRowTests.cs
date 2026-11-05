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
}
