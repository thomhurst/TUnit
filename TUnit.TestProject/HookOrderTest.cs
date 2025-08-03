namespace TUnit.TestProject;

public class HookOrderTests
{
    [Before(Test)]
    public void Setup()
    {
        Console.WriteLine("Before test setup");
    }

    [Test]
    public void Basic()
    {
        Console.WriteLine("This is a basic test");
    }
}

public sealed class GlobalHooks
{
    [BeforeEvery(Test)]
    public static void BeforeTest(TestContext testContext)
    {
        Console.WriteLine("Before every test");
    }
}