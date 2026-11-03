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
}
