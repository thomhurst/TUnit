using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class Dep;

[EngineTest(ExpectedResult.Pass)]
internal sealed class InternalClassWithInternalProperty
{
    [ClassDataSource<Dep>(Shared = SharedType.None)]
    internal required Dep Dep { get; init; }

    [Test]
    internal async Task TestMethod1()
    {
        await Assert.That(Dep).IsNotNull();
    }
}

[EngineTest(ExpectedResult.Pass)]
public sealed class PublicClassWithInternalProperty
{
    [ClassDataSource<Dep>(Shared = SharedType.None)]
    public required Dep Dep { get; init; }

    [Test]
    public async Task TestMethod1()
    {
        await Assert.That(Dep).IsNotNull();
    }
}

[EngineTest(ExpectedResult.Pass)]
public sealed class PublicClassWithPublicProperty
{
    [ClassDataSource<Dep>(Shared = SharedType.None)]
    public required Dep Dep { get; init; }

    [Test]
    public async Task TestMethod1()
    {
        await Assert.That(Dep).IsNotNull();
    }
}
