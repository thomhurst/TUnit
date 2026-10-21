using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7155;

// An injected CancellationToken between the data parameters takes no row value, so the null still reaches
// T's second parameter and the row infers object, the listed type, in both modes. It runs once as
// Run<object>, not also as Run<int>.
[EngineTest(ExpectedResult.Pass)]
public class NullAndValueTypeWithTokenGenerateGenericTestMethod
{
    // TUnit0001 only skips a trailing CancellationToken (#7173)
#pragma warning disable TUnit0001
    [Test]
    [GenerateGenericTest(typeof(object))]
    [Arguments(1, null)]
    public async Task Run<T>(T first, CancellationToken token, T second)
#pragma warning restore TUnit0001
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(object));
        await Assert.That(first).IsEqualTo(1);
        await Assert.That(second).IsNull();
    }
}

// A Nullable<T> inferred from a collection's element type can hold the null, so it stays int? rather than
// becoming object, which List<int?> could not be passed as.
[EngineTest(ExpectedResult.Pass)]
public class NullableCollectionAndNullGenericMethod
{
    public static IEnumerable<Func<(List<int?>, int?)>> Rows()
    {
        yield return () => ([1], null);
    }

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task Run<T>(List<T> values, T missing)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int?));
        await Assert.That(values).HasSingleItem();
        await Assert.That(missing).IsNull();
    }
}
