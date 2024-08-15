namespace TUnit.TestProject.AfterTests;

public class GlobalBase1
{
    [AfterEvery(EachTest)]
    public static async Task AfterAll1(TestContext context)
    {
        await Task.CompletedTask;
    }
    
    [After(EachTest)]
    public async Task AfterEach1()
    {
        await Task.CompletedTask;
    }
}

public class GlobalBase2 : GlobalBase1
{
    [AfterEvery(EachTest)]
    public static async Task AfterAll2(TestContext context)
    {
        await Task.CompletedTask;
    }
    
    [After(EachTest)]
    public async Task AfterEach2()
    {
        await Task.CompletedTask;
    }
}

public class GlobalBase3 : GlobalBase2
{
    [AfterEvery(EachTest)]
    public static async Task AfterAll3(TestContext context)
    {
        await Task.CompletedTask;
    }
    
    [After(EachTest)]
    public async Task AfterEach3()
    {
        await Task.CompletedTask;
    }
}

public class GlobalCleanUpTests : GlobalBase3
{
    [AfterEvery(EachTest)]
    public static async Task AfterAllCleanUp(TestContext context)
    {
        await Task.CompletedTask;
    }
    
    [AfterEvery(EachTest)]
    public static async Task AfterAllCleanUp(TestContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
        
    [AfterEvery(EachTest)]
    public static async Task AfterAllCleanUpWithContext(TestContext context)
    {
        await Task.CompletedTask;
    }
    
    [AfterEvery(EachTest)]
    public static async Task AfterAllCleanUpWithContext(TestContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    [After(EachTest)]
    public async Task CleanUp()
    {
        await Task.CompletedTask;
    }
    
    [After(EachTest), Timeout(30_000)]
    public async Task CleanUp(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    [After(EachTest)]
    public async Task CleanUpWithContext(TestContext testContext)
    {
        await Task.CompletedTask;
    }
    
    [After(EachTest), Timeout(30_000)]
    public async Task CleanUpWithContext(TestContext testContext, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}