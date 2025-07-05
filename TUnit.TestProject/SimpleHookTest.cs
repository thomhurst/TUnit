using TUnit.Core;
using System.Threading.Tasks;

namespace TUnit.TestProject;

public class SimpleHookTest
{
    private static int _beforeCount = 0;
    private static int _afterCount = 0;
    
    [Before(HookType.Test)]
    public void BeforeEachTestInstance()
    {
        _beforeCount++;
        Console.WriteLine($"Instance Before test hook executed. Count: {_beforeCount}");
    }
    
    [After(HookType.Test)]
    public void AfterEachTestInstance()
    {
        _afterCount++;
        Console.WriteLine($"Instance After test hook executed. Count: {_afterCount}");
    }
    
    [BeforeEvery(HookType.Test)]
    public static void BeforeEachTestStatic()
    {
        Console.WriteLine("Static BeforeEvery test hook executed");
    }
    
    [AfterEvery(HookType.Test)]
    public static void AfterEachTestStatic()
    {
        Console.WriteLine("Static AfterEvery test hook executed");
    }
    
    [Before(HookType.Class)]
    public static void BeforeClassHook()
    {
        Console.WriteLine("Before class hook executed");
    }
    
    [After(HookType.Class)]
    public static void AfterClassHook()
    {
        Console.WriteLine("After class hook executed");
    }
    
    [Test]
    public async Task SimpleTest1()
    {
        Console.WriteLine("SimpleTest1 executed");
        await Assert.That(_beforeCount).IsGreaterThan(0);
    }
    
    [Test]
    public async Task SimpleTest2()
    {
        Console.WriteLine("SimpleTest2 executed");
        await Assert.That(_beforeCount).IsGreaterThan(0);
    }
}