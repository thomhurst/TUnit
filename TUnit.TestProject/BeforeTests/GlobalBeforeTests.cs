using TUnit.Core;

namespace TUnit.TestProject.BeforeTests;

public class GlobalBase1
{
    [GlobalBeforeEachTest]
    public static async Task BeforeAll1()
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public async Task BeforeEach1()
    {
        await Task.CompletedTask;
    }
}

public class GlobalBase2 : GlobalBase1
{
    [GlobalBeforeEachTest]
    public static async Task BeforeAll2()
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public async Task BeforeEach2()
    {
        await Task.CompletedTask;
    }
}

public class GlobalBase3 : GlobalBase2
{
    [GlobalBeforeEachTest]
    public static async Task BeforeAll3()
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public async Task BeforeEach3()
    {
        await Task.CompletedTask;
    }
}

public class GlobalSetUpTests : GlobalBase3
{
    [GlobalBeforeEachTest]
    public static async Task BeforeAllSetUp()
    {
        await Task.CompletedTask;
    }
    
    [GlobalBeforeEachTest]
    public static async Task BeforeAllSetUp(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
        
    [GlobalBeforeEachTest]
    public static async Task BeforeAllSetUpWithContext(TestContext context)
    {
        await Task.CompletedTask;
    }
    
    [GlobalBeforeEachTest]
    public static async Task BeforeAllSetUpWithContext(TestContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public async Task SetUp()
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest, Timeout(30_000)]
    public async Task SetUp(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public async Task SetUpWithContext(TestContext testContext)
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest, Timeout(30_000)]
    public async Task SetUpWithContext(TestContext testContext, CancellationToken cancellationToken)
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