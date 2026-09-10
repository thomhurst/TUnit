using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3231;

[EngineTest(ExpectedResult.Pass)]
public partial class Tests
{
    [Test]
    public void Basic()
    {
        Console.WriteLine($"This is a basic test with data: {DataClass.Value}");
    }
}