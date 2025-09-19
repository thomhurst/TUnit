namespace TUnit.TestProject.Bugs;

public abstract class BaseTestForSourceLocationCheck
{
    [Test]
    public async Task BaseTestMethod()
    {
        await Assert.That(Environment.ProcessorCount).IsGreaterThan(0);
    }
}

[InheritsTests]
public sealed class DerivedTestForSourceLocationCheck : BaseTestForSourceLocationCheck
{
    [Test]
    public async Task DerivedTestMethod()
    {
        await Assert.That(Environment.ProcessorCount).IsGreaterThan(0);
    }
}