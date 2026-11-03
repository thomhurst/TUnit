using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7485;

[EngineTest(ExpectedResult.Pass)]
public class NestedTupleBindingTests
{
    public static (int, (int, string)) Row() => (1, (2, "nested"));

    [Test, MethodDataSource(nameof(Row))]
    public async Task Nested<T>((T, (int, object)) row)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row).IsEqualTo(((T)(object)1, (2, (object)"nested")));
    }

    [Test, MethodDataSource(nameof(Row))]
    public async Task Separate<T>(T first, (int, object) nested)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(first).IsEqualTo((T)(object)1);
        await Assert.That(nested).IsEqualTo((2, (object)"nested"));
    }

    public (int, (int, string)) InstanceRow() => Row();

    [Test, InstanceMethodDataSource(nameof(InstanceRow))]
    public async Task Wrapped((int, (int, object)) row)
    {
        await Assert.That(row).IsEqualTo((1, (2, (object)"nested")));
    }

    public static (int, (int, (string, int, int, int, int, int, int, string))) DeepRow()
        => (1, (2, ("three", 4, 5, 6, 7, 8, 9, "ten")));

    [Test, MethodDataSource(nameof(DeepRow))]
    public async Task Deep<T>((T, (int, (object, int, int, int, int, int, int, object))) row)
    {
        await Assert.That(row).IsEqualTo(((T)(object)1, (2, ((object)"three", 4, 5, 6, 7, 8, 9, (object)"ten"))));
    }

    public static IEnumerable<(int, (int, string)?)> NullableRows()
    {
        yield return (1, (2, "nullable"));
        yield return (0, null);
    }

    [Test, MethodDataSource(nameof(NullableRows))]
    public async Task NullableNested<T>((T, (int, object)?) row)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row.Item2.HasValue).IsEqualTo(EqualityComparer<T>.Default.Equals(row.Item1, (T)(object)1));
        if (row.Item2.HasValue)
        {
            await Assert.That(row.Item2.Value).IsEqualTo((2, (object)"nullable"));
        }
    }

    public static (int, (int, (int, (int, (int, string))))) VeryDeepRow()
        => (1, (2, (3, (4, (5, "six")))));

    [Test, MethodDataSource(nameof(VeryDeepRow))]
    public async Task VeryDeep<T>((T, (int, (int, (int, (int, object))))) row)
    {
        await Assert.That(row).IsEqualTo(((T)(object)1, (2, (3, (4, (5, (object)"six"))))));
    }
}
