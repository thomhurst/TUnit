namespace TUnit.TestProject.Bugs._3813;

[InheritsTests]
public class ImplementationATests : BaseServiceTests
{
    [Test]
    [Skip("Implementation A does not support advanced feature")]
    public new Task AdvancedFeature()
    {
        return Task.CompletedTask;
    }
}
