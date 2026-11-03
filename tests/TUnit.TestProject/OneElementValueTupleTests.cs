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

    public static IEnumerable<TestDataRow<ValueTuple<object>>> WrappedNestedRows() =>
        [new(new ValueTuple<object>(new ValueTuple<object>("x")))];

    public IEnumerable<TestDataRow<ValueTuple<object>>> InstanceWrappedNestedRows() => WrappedNestedRows();

    [Test]
    [MethodDataSource(nameof(WrappedNestedRows))]
    public Task WrappedNestedElement(ValueTuple<object> value) => NestedElement(value);

    [Test]
    [InstanceMethodDataSource(nameof(InstanceWrappedNestedRows))]
    public Task WrappedNestedElementFromInstance(ValueTuple<object> value) => NestedElement(value);

    public static object GenericNestedRow() => new ValueTuple<object>(new ValueTuple<object>("x"));

    public object InstanceGenericNestedRow() => GenericNestedRow();

    [Test]
    [GenerateGenericTest(typeof(ValueTuple<object>))]
    [MethodDataSource(nameof(GenericNestedRow))]
    public Task GenericNestedElement<T>(T value) => NestedElement((ValueTuple<object>)(object)value!);

    [Test]
    [GenerateGenericTest(typeof(ValueTuple<object>))]
    [InstanceMethodDataSource(nameof(InstanceGenericNestedRow))]
    public Task GenericNestedElementFromInstance<T>(T value) => NestedElement((ValueTuple<object>)(object)value!);
    public static IEnumerable<TestDataRow<ValueTuple<(int, int)>>> WrappedPairRows() =>
        [new(new ValueTuple<(int, int)>((1, 2)))];

    public IEnumerable<TestDataRow<ValueTuple<(int, int)>>> InstanceWrappedPairRows() => WrappedPairRows();

    [Test]
    [MethodDataSource(nameof(WrappedPairRows))]
    public async Task WrappedPairExpandsOuterTuple((int, int) value)
    {
        await Assert.That(value).IsEqualTo((1, 2));
    }

    [Test]
    [InstanceMethodDataSource(nameof(InstanceWrappedPairRows))]
    public Task WrappedPairExpandsOuterTupleFromInstance((int, int) value) => WrappedPairExpandsOuterTuple(value);
}
