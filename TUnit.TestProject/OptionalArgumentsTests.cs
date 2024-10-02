namespace TUnit.TestProject;

public class OptionalArgumentsTests
{
    [Test]
    [Arguments(1)]
    public void Test(int value, bool flag = true)
    {
        // Dummy Method
    }
}