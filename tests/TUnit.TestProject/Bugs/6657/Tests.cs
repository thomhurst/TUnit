namespace TUnit.TestProject.Bugs._6657;

public interface IThing;

public sealed class Thing : IThing;

public abstract class BaseTests
{
    public abstract IThing Thing { get; }

    [Test]
    public async Task InheritedTest()
    {
        await Assert.That(Thing).IsTypeOf<Thing>();
    }
}

[InheritsTests]
public sealed class CovariantPropertyOverrideTests : BaseTests
{
    public override Thing Thing { get; } = new();
}
