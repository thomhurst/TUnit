using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7399;

[EngineTest(ExpectedResult.Pass)]
public class TypedArrayArgumentsTests
{
    [Test]
    [Arguments<int[]>(new[] { 1, 2 })]
    public async Task Array<T>(T[] values)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(values.Length).IsEqualTo(2);
    }
}
