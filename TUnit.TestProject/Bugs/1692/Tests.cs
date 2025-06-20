using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1692;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    private const string? NullContent = null;

    [Test]
    [Arguments(NullContent)]
    [Arguments(null)]
    public async Task NullTest(string? t) => await Assert.That(t).IsNull();
}