using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7514;

[EngineTest(ExpectedResult.Pass)]
public class PreservedGenericTupleTests
{
    public (int, (int, string)) InstanceRow() => (1, (2, "nested"));

    [Test, GenerateGenericTest(typeof(int)), InstanceMethodDataSource(nameof(InstanceRow))]
    public async Task Wrapped<T>((T, (int, object)) row)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(row).IsEqualTo(((T)(object)1, (2, (object)"nested")));
    }

    [Test, GenerateGenericTest(typeof(int)), InstanceMethodDataSource(nameof(InstanceRow))]
    public async Task WithToken<T>(CancellationToken token, (T, (int, object)) row)
    {
        await Wrapped(row);
        await Assert.That(token.CanBeCanceled).IsTrue();
    }

    [Test, GenerateGenericTest(typeof(int)), InstanceMethodDataSource(nameof(InstanceRow))]
    public async Task Separate<T>(T first, (int, object) nested)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(first).IsEqualTo((T)(object)1);
        await Assert.That(nested).IsEqualTo((2, (object)"nested"));
    }

    [Test, GenerateGenericTest(typeof(int)), InstanceMethodDataSource(nameof(InstanceRow))]
    public async Task Scalar<T>(T first)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(first).IsEqualTo((T)(object)1);
    }
}
