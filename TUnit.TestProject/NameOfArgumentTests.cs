namespace TUnit.TestProject;

public class NameOfArgumentTests
{
    [Test]
    [Arguments(nameof(TestName))]
    public void TestName(string name)
    {
        // Dummy method
    }
}