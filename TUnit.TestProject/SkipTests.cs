namespace TUnit.TestProject;

public class SkipTests
{
    [Test]
    [Skip("Just because.")]
    public void Test()
    {
    }
}