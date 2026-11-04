using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7573;

[EngineTest(ExpectedResult.Pass)]
public class NullableClassMethodRows<T> where T : struct
{
    public static IEnumerable<U> Values<U>() => [];
    public static IEnumerable<int> Values() => [42];
    public static IEnumerable<int> MoreValues() => [42];
    public static IEnumerable<double> DoubleValues() => [84d];
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
    [MethodDataSource(nameof(Values))]
    [MethodDataSource(nameof(DoubleValues))]
    [MethodDataSource(nameof(MoreValues))]
    public async Task MultipleSources(T? value)
    {
        // Each source runs only on the class type inferred from its declared row type.
        await Assert.That(value).IsEqualTo(typeof(T) == typeof(int) ? (T)(object)42 : (T)(object)84d);
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
