using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7224;

[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(nameof(Rows))]
public class WrappedArrayConstructorScalarInferenceTests<T>(T value) where T : IComparable<T>
{
    public static IEnumerable<TestDataRow<string[]>> Rows() => [new(new[] { "one" })];

    // Seed generator class inference; the class source must still match this instantiation.
    [Test, Arguments("seed")]
    public async Task ConstructorScalar(T seed)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(value).IsEqualTo((T)(object)"one");
    }
}

[EngineTest(ExpectedResult.Pass)]
[MethodDataSource(nameof(Rows))]
public class WrappedArrayConstructorPairInferenceTests<T, U>(T first, U second)
{
    public static IEnumerable<TestDataRow<string[]>> Rows() => [new(new[] { "one", "two" })];

    [Test, Arguments("seed", "seed")]
    public async Task ConstructorPair(T seed, U otherSeed)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(typeof(U)).IsEqualTo(typeof(string));
        await Assert.That(first).IsEqualTo((T)(object)"one");
        await Assert.That(second).IsEqualTo((U)(object)"two");
    }
}
