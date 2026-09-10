using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[Repeat(3)]
public class RepeatTests
{
    [Test]
    [Repeat(1)]
    public void One()
    {
        // Dummy method
    }

    [Test]
    [Repeat(2)]
    public void Two()
    {
        // Dummy method
    }

    [Test]
    public void Three()
    {
        // Dummy method
    }
}
