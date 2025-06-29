namespace TUnit.TestProject.BeforeTests;

public class AssemblyBase1
{
    [Before(Assembly)]
    public static async Task BeforeAll1()
    {
        await Task.CompletedTask;
    }

    [Before(Test)]
    public async Task BeforeEach1()
    {
        await Task.CompletedTask;
    }
}

public class AssemblyBase2 : AssemblyBase1
{
    [Before(Assembly)]
    public static async Task BeforeAll2()
    {
        await Task.CompletedTask;
    }

    [Before(Test)]
    public async Task BeforeEach2()
    {
        await Task.CompletedTask;
    }
}

public class AssemblyBase3 : AssemblyBase2
{
    [Before(Assembly)]
    public static async Task BeforeAll3()
    {
        await Task.CompletedTask;
    }

    [Before(Test)]
    public async Task BeforeEach3()
    {
        await Task.CompletedTask;
    }
}

public class AssemblySetupTests : AssemblyBase3
{
    [Before(Assembly)]
    public static async Task BeforeAllSetUp()
    {
        await Task.CompletedTask;
    }

    [Before(Assembly)]
    public static async Task BeforeAllSetUpWithContext(AssemblyHookContext context)
    {
        await Task.CompletedTask;
    }

    [Before(Assembly)]
    public static async Task BeforeAllSetUp(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    [Before(Assembly)]
    public static async Task BeforeAllSetUpWithContext(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    [Before(Test)]
    public async Task Setup()
    {
        await Task.CompletedTask;
    }

    [Before(Test), Timeout(30_000)]
    public async Task Setup(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    [Before(Test)]
    public async Task SetupWithContext(TestContext testContext)
    {
        await Task.CompletedTask;
    }

    [Before(Test), Timeout(30_000)]
    public async Task SetupWithContext(TestContext testContext, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}
