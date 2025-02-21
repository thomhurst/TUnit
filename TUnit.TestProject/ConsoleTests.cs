namespace TUnit.TestProject;

public class ConsoleTests
{
    [Test]
    public void Write_Source_Gen_Information()
    {
        Console.WriteLine(TestContext.Current!.TestDetails.TestMethod);
    }
}