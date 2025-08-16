using TUnit.Core;

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
        // Only output for our specific test class to avoid polluting other tests
        if (testContext.TestDetails.ClassType == typeof(HookOrderTests))
        {
            Console.WriteLine("Before every test");
        }
    }
}