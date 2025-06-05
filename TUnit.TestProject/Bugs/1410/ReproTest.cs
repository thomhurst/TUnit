using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1410;

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<SharedFixture>(Shared = SharedType.PerTestSession)]
public class ReproTest(SharedFixture fixture)
{
    [Test]
    public async Task ReproTest1()
    {
        await Assert.That(fixture.IsDisposed).IsFalse();
    }

    [Test, DependsOn(nameof(ReproTest1))]
    public async Task ReproTest2()
    {
        await Assert.That(fixture.IsDisposed).IsFalse();
    }
}