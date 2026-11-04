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

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string), typeof(int))]
[MethodDataSource(nameof(Rows))]
public class WrappedArrayConstructorOmittedArrayTests<T, U>(T first, U[] rest) where U : struct
{
    public static IEnumerable<TestDataRow<string[]>> Rows() => [new(new[] { "one" })];

    [Test]
    public async Task ExplicitTypesPreserveOmittedArray()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(typeof(U)).IsEqualTo(typeof(int));
        await Assert.That(first).IsEqualTo((T)(object)"one");
        await Assert.That(rest).IsEmpty();
    }
}

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(object))]
[MethodDataSource(nameof(Rows))]
public class WrappedArrayConstructorExplicitAndInferredTests<T>(params T[] values)
{
    public static IEnumerable<TestDataRow<string[]>> Rows() => [new(new[] { "one" })];

    // Seed generator class inference, as in the other constructor fixtures (#7333).
    [Test, Arguments("seed")]
    public async Task InferredTypesRemainAvailable(T seed)
    {
        await Assert.That(typeof(T) == typeof(object) || typeof(T) == typeof(string)).IsTrue();
        await Assert.That(values.Length).IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo((T)(object)"one");
    }
}

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string), typeof(int))]
[Arguments("row", new int[0])]
[Arguments(2, new int[0])]
[MethodDataSource(nameof(Rows))]
public class ExplicitClassDeferredRowTests<T, U>(T first, U[] rest) where U : struct
{
    public static IEnumerable<TestDataRow<string[]>> Rows() => [new(new[] { "deferred" })];

    [Test, GenerateGenericTest(typeof(bool))]
    public async Task GenericMethodPreservesExplicitClassRow<V>()
    {
        await Assert.That(typeof(V)).IsEqualTo(typeof(bool));
        if (typeof(T) == typeof(int))
        {
            await Assert.That(first).IsEqualTo((T)(object)2);
        }
        else
        {
            await Assert.That((string)(object)first!).IsIn("row", "deferred");
        }
        await Assert.That(rest).IsEmpty();
    }
}

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string), typeof(int))]
[MethodDataSource(nameof(Rows))]
public class WrappedArrayConstructorOmittedOptionalTests<T, U>(T first, U second = default) where U : struct
{
    public static IEnumerable<TestDataRow<string[]>> Rows() => [new(new[] { "one" })];

    [Test]
    public async Task ExplicitTypesPreserveOmittedOptional()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(typeof(U)).IsEqualTo(typeof(int));
        await Assert.That(first).IsEqualTo((T)(object)"one");
        await Assert.That(second).IsEqualTo(default(U));
    }
}
