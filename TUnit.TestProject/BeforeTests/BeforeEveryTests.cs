namespace TUnit.TestProject.BeforeTests;

public class GlobalBase1
{
    [BeforeEvery(EachTest)]
    public static async Task BeforeAll1(TestContext context)
    {
        await Task.CompletedTask;
    }
    
    [Before(EachTest)]
    public async Task BeforeEach1()
    {
        await Task.CompletedTask;
    }
}

public class GlobalBase2 : GlobalBase1
{
    [BeforeEvery(EachTest)]
    public static async Task BeforeAll2(TestContext context)
    {
        await Task.CompletedTask;
    }
    
    [Before(EachTest)]
    public async Task BeforeEach2()
    {
        await Task.CompletedTask;
    }
}

public class GlobalBase3 : GlobalBase2
{
    [BeforeEvery(EachTest)]
    public static async Task BeforeAll3(TestContext context)
    {
        await Task.CompletedTask;
    }
    
    [Before(EachTest)]
    public async Task BeforeEach3()
    {
        await Task.CompletedTask;
    }
}

public class GlobalSetUpTests : GlobalBase3
{
    [BeforeEvery(EachTest)]
    public static async Task BeforeAllSetUp(TestContext context)
    {
        await Task.CompletedTask;
    }
    
    [BeforeEvery(EachTest)]
    public static async Task BeforeAllSetUp(TestContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
        
    [BeforeEvery(EachTest)]
    public static async Task BeforeAllSetUpWithContext(TestContext context)
    {
        await Task.CompletedTask;
    }
    
    [BeforeEvery(EachTest)]
    public static async Task BeforeAllSetUpWithContext(TestContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    [Before(EachTest)]
    public async Task SetUp()
    {
        await Task.CompletedTask;
    }
    
    [Before(EachTest), Timeout(30_000)]
    public async Task SetUp(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    [Before(EachTest)]
    public async Task SetUpWithContext(TestContext testContext)
    {
        await Task.CompletedTask;
    }
    
    [Before(EachTest), Timeout(30_000)]
    public async Task SetUpWithContext(TestContext testContext, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}