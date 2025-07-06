namespace TUnit.TestProject;

public class HookGeneratorTest
{
    private static int _beforeTestCount = 0;
    private static int _afterTestCount = 0;

    [BeforeEvery(HookType.Test)]
    public static void BeforeEachTest()
    {
        _beforeTestCount++;
        Console.WriteLine($"BeforeEvery test hook executed. Count: {_beforeTestCount}");
    }

    [AfterEvery(HookType.Test)]
    public static async Task AfterEachTest()
    {
        _afterTestCount++;
        await Task.Delay(1);
        Console.WriteLine($"AfterEvery test hook executed. Count: {_afterTestCount}");
    }

    [Before(HookType.Class)]
    public static void BeforeClass(ClassHookContext context)
    {
        Console.WriteLine($"Before class hook executed for: {context.ClassType.Name}");
    }

    [After(HookType.Class)]
    public static ValueTask AfterClass(ClassHookContext context)
    {
        Console.WriteLine($"After class hook executed for: {context.ClassType.Name}");
        return default;
    }

    [Test]
    public async Task TestWithHooks1()
    {
        Console.WriteLine("Test 1 executed");
        await Assert.That(_beforeTestCount).IsGreaterThan(0);
    }

    [Test]
    public async Task TestWithHooks2()
    {
        Console.WriteLine("Test 2 executed");
        await Assert.That(_beforeTestCount).IsGreaterThan(0);
    }
}
