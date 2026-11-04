using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7451;

// Mixed-source type-free-row parity is tracked separately by #7390; see the shared
// inference matrix in docs/internal/generic-method-inference.md.

// Parameter inference owns all method rows on every class-row instantiation. The explicit method
// type repeats the inferred type, so it must not multiply the two class rows by two matrix values.
[EngineTest(ExpectedResult.Pass)]
[Arguments("first")]
[Arguments("second")]
public class ClassAndMethodParameterInference<T>(T classValue)
{
    [Test]
    [GenerateGenericTest(typeof(int))]
    [MatrixDataSource]
    public async Task Run<TMethod>([Matrix<int>(3, 4)] TMethod value)
    {
        Console.WriteLine($"Issue7451 class/method pair: {classValue},{value}");

        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That((object?)classValue is "first" or "second").IsTrue();
        await Assert.That(typeof(TMethod)).IsEqualTo(typeof(int));
        await Assert.That((object?)value is 3 or 4).IsTrue();
    }
}
