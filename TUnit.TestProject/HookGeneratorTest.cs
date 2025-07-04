using TUnit.Core;
using System.Threading.Tasks;

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
    public static void BeforeClass(HookContext context)
    {
        Console.WriteLine($"Before class hook executed for: {context.TestClassType.Name}");
    }
    
    [After(HookType.Class)]
    public static ValueTask AfterClass(HookContext context)
    {
        Console.WriteLine($"After class hook executed for: {context.TestClassType.Name}");
        return default;
    }
    
    [Test]
    public void TestWithHooks1()
    {
        Console.WriteLine("Test 1 executed");
        Assert.That(_beforeTestCount).IsGreaterThan(0);
    }
    
    [Test]
    public void TestWithHooks2()
    {
        Console.WriteLine("Test 2 executed");
        Assert.That(_beforeTestCount).IsGreaterThan(0);
    }
}