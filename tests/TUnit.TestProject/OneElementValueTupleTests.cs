using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class OneElementValueTupleTests
{
    public static IEnumerable<ValueTuple<string?>> Rows() =>
        [new("a"), new((string?)null)];

    public IEnumerable<ValueTuple<string?>> InstanceRows() => Rows();

    [Test]
    [InstanceMethodDataSource(nameof(InstanceRows))]
    public async Task SingleElementFromInstance(ValueTuple<string?> value)
    {
        await Assert.That(value.Item1 is null or "a").IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task SingleElement(ValueTuple<string?> value)
    {
        await Assert.That(value.Item1 is null or "a").IsTrue();
    }

    public static IEnumerable<ValueTuple<object>> NestedRows() =>
        [new(new ValueTuple<object>("x"))];

    public IEnumerable<ValueTuple<object>> InstanceNestedRows() => NestedRows();

    [Test]
    [MethodDataSource(nameof(NestedRows))]
    public async Task NestedElement(ValueTuple<object> value)
    {
        await Assert.That(value.Item1).IsTypeOf<ValueTuple<object>>();
        await Assert.That(((ValueTuple<object>)value.Item1).Item1).IsEqualTo("x");
    }

    [Test]
    [InstanceMethodDataSource(nameof(InstanceNestedRows))]
    public async Task NestedElementFromInstance(ValueTuple<object> value)
    {
        await Assert.That(value.Item1).IsTypeOf<ValueTuple<object>>();
        await Assert.That(((ValueTuple<object>)value.Item1).Item1).IsEqualTo("x");
    }
}
