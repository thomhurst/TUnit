namespace TUnit.TestProject;

public class SimpleSkipTest
{
    static SimpleSkipTest()
    {
        Console.WriteLine("Static constructor called for SimpleSkipTest");
    }

    public SimpleSkipTest()
    {
        Console.WriteLine("CONSTRUCTOR CALLED FOR SKIPPED TEST - This should NOT appear!");
    }

    [Test]
    [Skip("This test should be skipped")]
    public void BasicSkippedTest()
    {
        Console.WriteLine("This test method should not run");
    }
}