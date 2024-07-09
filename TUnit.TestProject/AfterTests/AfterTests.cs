using TUnit.Core;

namespace TUnit.TestProject.AfterTests;

public class Base1
{
    [AfterAllTestsInClass]
    public static async Task AfterAll1()
    {
        await Task.CompletedTask;
    }
    
    [AfterEachTest]
    public async Task AfterEach1()
    {
        await Task.CompletedTask;
    }
}

public class Base2 : Base1
{
    [AfterAllTestsInClass]
    public static async Task AfterAll2()
    {
        await Task.CompletedTask;
    }
    
    [AfterEachTest]
    public async Task AfterEach2()
    {
        await Task.CompletedTask;
    }
}

public class Base3 : Base2
{
    [AfterAllTestsInClass]
    public static async Task AfterAll3()
    {
        await Task.CompletedTask;
    }
    
    [AfterEachTest]
    public async Task AfterEach3()
    {
        await Task.CompletedTask;
    }
}

public class CleanupTests : Base3
{
    [AfterAllTestsInClass]
    public static async Task AfterAllCleanup()
    {
        await Task.CompletedTask;
    }
    
    [AfterEachTest]
    public async Task Cleanup()
    {
        await Task.CompletedTask;
    }
    
    [AfterEachTest]
    public async Task CleanupWithContext(TestContext testContext)
    {
        await Task.CompletedTask;
    }

    [Test]
    public async Task Test1()
    {
        await Task.CompletedTask;
    }
    
    [Test]
    public async Task Test2()
    {
        await Task.CompletedTask;
    }
}