using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7224Reflection;

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(object))]
[MethodDataSource(nameof(Rows))]
public class WrappedArrayConstructorInferenceTests<T>(params T[] values)
{
    public static IEnumerable<TestDataRow<string[]>> Rows() => [new(new[] { "one" })];

    [Test]
    public async Task SuppliedArrayInfersAlongsideExplicitTypes()
    {
        await Assert.That(typeof(T) == typeof(object) || typeof(T) == typeof(string)).IsTrue();
        await Assert.That(values.Length).IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo((T)(object)"one");
    }
}
