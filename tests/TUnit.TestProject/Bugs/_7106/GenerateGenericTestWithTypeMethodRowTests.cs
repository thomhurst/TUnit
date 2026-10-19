using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7106;

// A Type value in a method-level [Arguments] row infers System.Type, not the private RuntimeType it is
// an instance of, so it lands on the instantiation [GenerateGenericTest(typeof(Type))] lists and runs
// once, in both modes (#7106).
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(Type))]
public class GenerateGenericTestWithTypeMethodRow<T>
{
    [Test]
    [Arguments(typeof(int))]
    public async Task Run(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(Type));
        await Assert.That((object?)value).IsEqualTo(typeof(int));
    }
}
