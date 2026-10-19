using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7106;

// [GenerateGenericTest] adds instantiations without suppressing those that method-level [Arguments]
// rows infer: a row inferring a type the attribute does not list still runs, and a row inferring a
// type the attribute does list runs on that instantiation alone, in both modes (#7106).
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
public class GenerateGenericTestWithMethodRows<T>
{
    [Test]
    [Arguments(1)]
    [Arguments("a")]
    public async Task Run(T value)
    {
        await Assert.That(value).IsTypeOf<T>();
    }
}
