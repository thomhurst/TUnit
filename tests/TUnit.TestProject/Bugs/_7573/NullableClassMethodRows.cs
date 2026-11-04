using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7573;

[EngineTest(ExpectedResult.Pass)]
public class NullableClassMethodRows<T> where T : struct
{
    public static IEnumerable<int> Values() => [42];
    public static IEnumerable<int?> NullableValues() => [42, null];
    public static IEnumerable<Func<int>> Factories() => [() => 42];
    public static IEnumerable<Func<(int, int?)>> Tuples() => [() => (42, null)];

    [Test]
    [MethodDataSource(nameof(Values))]
    public async Task Value(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsNotNull();
    }

    [Test]
    [MethodDataSource(nameof(NullableValues))]
    public async Task NullableValue(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value is null || value.Equals(42)).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Factories))]
    public async Task Factory(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsNotNull();
    }

    [Test]
    [MethodDataSource(nameof(Tuples))]
    public async Task Tuple(T? first, T? second)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(first).IsNotNull();
        await Assert.That(second).IsNull();
    }
}
