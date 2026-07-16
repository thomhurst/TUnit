namespace TUnit.TestProject.BeforeTests;

public class Base1
{
    [Before(Class)]
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

public class Base2 : Base1
{
    [Before(Class)]
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

public class Base3 : Base2
{
    [Before(Class)]
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

public class SetupTests : Base3
{
    [Before(Class)]
    public static async Task BeforeAllSetUp()
    {
        await Task.CompletedTask;
    }

    [Before(Class)]
    public static async Task BeforeAllSetUpWithContext(ClassHookContext context)
    {
        await Task.CompletedTask;
    }

    [Before(Class)]
    public static async Task BeforeAllSetUp(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    [Before(Class)]
    public static async Task BeforeAllSetUpWithContext(ClassHookContext context, CancellationToken cancellationToken)
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
