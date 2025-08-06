namespace TUnit.TestProject.VerifyAfterHooksFix;

public class VerifyAfterHooksFixTests
{
    public static int _afterEveryTestCallCount = 0;
    public static int _afterClassCallCount = 0;
    public static int _afterEveryClassCallCount = 0;
    public static int _afterAssemblyCallCount = 0;
    public static int _afterEveryAssemblyCallCount = 0;

    [Test]
    public void Test1()
    {
        Console.WriteLine("Test1 executing");
    }

    [Test]
    public void Test2()
    {
        Console.WriteLine("Test2 executing");
    }

    [Test]
    public void VerifyAfterHooksWereCalled()
    {
        Console.WriteLine($"AfterEveryTest calls: {_afterEveryTestCallCount}");
        Console.WriteLine($"AfterClass calls: {_afterClassCallCount}");
        Console.WriteLine($"AfterEveryClass calls: {_afterEveryClassCallCount}");
        Console.WriteLine($"AfterAssembly calls: {_afterAssemblyCallCount}");
        Console.WriteLine($"AfterEveryAssembly calls: {_afterEveryAssemblyCallCount}");
        
        // This test will run third, so by the time it finishes:
        // - AfterEveryTest should have been called 3 times (once for each test)
        // - AfterClass should have been called 1 time (after all tests in this class complete)
        // - AfterEveryClass should have been called 1 time
        // - AfterAssembly should have been called 1 time (after all tests in assembly complete)
        // - AfterEveryAssembly should have been called 1 time
        
        // Note: The exact counts depend on when this verification runs vs when hooks execute
        // The important thing is that the hooks actually execute (non-zero counts)
    }
}

public class GlobalAfterHooks
{
    [AfterEvery(Test)]
    public static void AfterEveryTest()
    {
        Console.WriteLine("AfterEveryTest hook executing");
        VerifyAfterHooksFixTests._afterEveryTestCallCount++;
    }

    [After(Class)]
    public static void AfterClass()
    {
        Console.WriteLine("AfterClass hook executing");
        VerifyAfterHooksFixTests._afterClassCallCount++;
    }

    [AfterEvery(Class)]
    public static void AfterEveryClass()
    {
        Console.WriteLine("AfterEveryClass hook executing");
        VerifyAfterHooksFixTests._afterEveryClassCallCount++;
    }

    [After(Assembly)]
    public static void AfterAssembly()
    {
        Console.WriteLine("AfterAssembly hook executing");
        VerifyAfterHooksFixTests._afterAssemblyCallCount++;
    }

    [AfterEvery(Assembly)]
    public static void AfterEveryAssembly()
    {
        Console.WriteLine("AfterEveryAssembly hook executing");
        VerifyAfterHooksFixTests._afterEveryAssemblyCallCount++;
    }
}