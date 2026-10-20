using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7121;

[EngineTest(ExpectedResult.Pass)]
public class RepeatedTypeParameterTests
{
    public static IEnumerable<Func<(string, int)>> Rows()
    {
        yield return () => ("1", 2);
    }

    // T occurs twice. Source-gen mode infers T from its last occurrence (int) and converts "1"
    // through CastHelper. Reflection mode must instantiate the same type it reports.
    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task Repeated<T>(T a, T b) where T : struct
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(a).IsEqualTo((T)(object)1);
        await Assert.That(b).IsEqualTo((T)(object)2);
    }
}
