using TUnit.Core;
using TUnit.Core.Models;

namespace TUnit.TestProject.AfterTests;

public class AssemblyBase1
{
    [AssemblyCleanUp]
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

public class AssemblyBase2 : AssemblyBase1
{
    [AssemblyCleanUp]
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

public class AssemblyBase3 : AssemblyBase2
{
    [AssemblyCleanUp]
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

public class AssemblyCleanupTests : AssemblyBase3
{
    [AssemblyCleanUp]
    public static async Task AfterAllCleanUp()
    {
        await Task.CompletedTask;
    }
        
    [AssemblyCleanUp]
    public static async Task AfterAllCleanUpWithContext(AssemblyHookContext context)
    {
        await Task.CompletedTask;
    }
    
    [AssemblyCleanUp]
    public static async Task AfterAllCleanUp(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    [AssemblyCleanUp]
    public static async Task AfterAllCleanUpWithContext(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    [AfterEachTest]
    public async Task Cleanup()
    {
        await Task.CompletedTask;
    }
    
    [AfterEachTest, Timeout(30_000)]
    public async Task Cleanup(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    [AfterEachTest]
    public async Task CleanupWithContext(TestContext testContext)
    {
        await Task.CompletedTask;
    }
    
    [AfterEachTest, Timeout(30_000)]
    public async Task CleanupWithContext(TestContext testContext, CancellationToken cancellationToken)
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