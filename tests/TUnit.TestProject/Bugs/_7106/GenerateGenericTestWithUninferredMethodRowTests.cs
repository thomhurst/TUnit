using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7106;

// A method-level row that infers no type (here a null value) still runs on the instantiation a
// [GenerateGenericTest] lists, even when another row infers that same instantiation, in both modes.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string))]
public class GenerateGenericTestWithUninferredMethodRow<T>
{
    [Test]
    [Arguments("a")]
    [Arguments(null)]
    public async Task Run(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(value is null or "a").IsTrue();
    }
}
