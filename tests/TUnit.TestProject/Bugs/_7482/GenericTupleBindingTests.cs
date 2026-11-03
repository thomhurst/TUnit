using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7482;

[EngineTest(ExpectedResult.Pass)]
public class GenericTupleBindingTests
{
    public static (int, string) Row() => (1, "text");

    [Test, MethodDataSource(nameof(Row))]
    public async Task Widened<T>((T, object) row)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row.Item1).IsEqualTo((T)(object)1);
        await Assert.That(row.Item2).IsEqualTo("text");
    }

    [Test, MethodDataSource(nameof(Row))]
    public async Task Compatible<T>((T, string) row)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row.Item1).IsEqualTo((T)(object)1);
        await Assert.That(row.Item2).IsEqualTo("text");
    }

    public static (int, string, int, int, int, int, int, string) LongRow() => (1, "two", 3, 4, 5, 6, 7, "eight");

    [Test, MethodDataSource(nameof(LongRow))]
    public async Task Long<T>((T, object, int, int, int, int, int, object) row)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row).IsEqualTo(((T)(object)1, (object)"two", 3, 4, 5, 6, 7, (object)"eight"));
    }

    [Test, MethodDataSource(nameof(Row))]
    public async Task WithToken<T>(CancellationToken token, (T, object) row)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row.Item1).IsEqualTo((T)(object)1);
        await Assert.That(row.Item2).IsEqualTo("text");
        await Assert.That(token.CanBeCanceled).IsTrue();
    }
}
